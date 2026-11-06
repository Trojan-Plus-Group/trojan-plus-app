# SPDX-License-Identifier: GPL-3.0-or-later
"""Host JNI boundary check. Requires JDK 17 and cc; no emulator or core edits."""
import os
from pathlib import Path
import re
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
source = (root / 'TrojanPlusApp.Android/TrojanPlusVPNService.cs').read_text()
name = re.search(r'JNIEnv.FindClass\("([^"]+)"\)', source).group(1)
registered = re.search(r'\[Register\("([^"]+)"\)', source).group(1)
java = root / 'TrojanPlusApp.Android/obj/Debug/net10.0-android/android/src/com/trojan_plus/android/TrojanPlusVPNService.java'
peer = java.read_text()
assert 'package com.trojan_plus.android;' in peer
assert 'public class TrojanPlusVPNService' in peer
assert 'public static void protectSocket (int p0)' in peer
jdk = Path(os.environ.get('JAVA_HOME', '/home/hermes/toolchains/jdk-17'))

with tempfile.TemporaryDirectory(prefix='jni-service-') as tmp:
    tmp = Path(tmp)
    # Stand-in removes Android inheritance only; peer name and callback signature above are checked.
    p = tmp / 'com/trojan_plus/android/TrojanPlusVPNService.java'
    p.parent.mkdir(parents=True)
    p.write_text('package com.trojan_plus.android; public class TrojanPlusVPNService { public static void protectSocket(int fd) {} }')
    subprocess.run([str(jdk / 'bin/javac'), str(p)], check=True)
    c = tmp / 'probe.c'
    c.write_text(r'''
#include <jni.h>
int main(int argc, char **argv) {
    JavaVM *vm; JNIEnv *env;
    JavaVMOption options[] = {{"-Djava.class.path=.", 0}, {"-Xcheck:jni", 0}};
    JavaVMInitArgs args = {JNI_VERSION_1_8, 2, options, JNI_FALSE};
    if (argc != 2 || JNI_CreateJavaVM(&vm, (void **)&env, &args)) return 2;
    jclass local = (*env)->FindClass(env, argv[1]);
    if ((*env)->ExceptionCheck(env)) {
        (*env)->ExceptionClear(env);
        (*vm)->DestroyJavaVM(vm);
        return 3;
    }
    if (!local) return 4;
    jclass global = (*env)->NewGlobalRef(env, local);
    (*env)->DeleteLocalRef(env, local);
    jmethodID method = (*env)->GetStaticMethodID(env, global, "protectSocket", "(I)V");
    if ((*env)->ExceptionCheck(env) || !method) return 5;
    (*env)->CallStaticVoidMethod(env, global, method, 42);
    if ((*env)->ExceptionCheck(env)) return 6;
    (*env)->DeleteGlobalRef(env, global);
    return (*vm)->DestroyJavaVM(vm) == JNI_OK ? 0 : 7;
}
''')
    subprocess.run(['cc', '-Wall', '-Wextra', '-Werror', '-I' + str(jdk / 'include'),
                    '-I' + str(jdk / 'include/linux'), str(c), '-L' + str(jdk / 'lib/server'),
                    '-Wl,-rpath,' + str(jdk / 'lib/server'), '-ljvm', '-o', str(tmp / 'probe')], check=True)
    old = subprocess.run([str(tmp / 'probe'), registered], cwd=tmp).returncode
    assert old == 3, f'Old dotted name must fail JNI lookup: {old}'
    new = subprocess.run([str(tmp / 'probe'), name], cwd=tmp).returncode
    assert new == 0, f'Production name must resolve and invoke callback: {new}'
    assert name == registered.replace('.', '/')

worker = source[source.index('        private class WorkerThread'):source.index('        private class VpnServiceHandler')]
assert worker.index('try') < worker.index('JNIEnv.NewString(')
assert worker.index('jclass == IntPtr.Zero') < worker.index('RunMain(JNIEnv.Handle, jclass, path);')
assert worker.index('RunMain(JNIEnv.Handle, jclass, path);') < worker.index('finally') < worker.index('JNIEnv.DeleteGlobalRef(jclass);')
assert 'JNIEnv.DeleteLocalRef(path);' in worker
assert 'catch (Exception)' in worker and 'Log.Error(TAG, "Native VPN worker failed.");' in worker
assert 'service.FinishStop();' in worker
print('PASS: old dotted JNI name rejected; production slash name resolves under -Xcheck:jni, global reference invokes protectSocket; worker ownership/error wiring checked.')
