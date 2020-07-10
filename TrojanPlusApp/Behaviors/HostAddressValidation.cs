using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace TrojanPlusApp.Behaviors
{
    public class HostAddressValidation : Behavior<Entry>
    {
        public void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            ((Entry)sender).TextColor = IsValid(args.NewTextValue) ? Color.Default : Color.Red;
        }

        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnEntryTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnEntryTextChanged;
            base.OnDetachingFrom(entry);
        }

        private static readonly string DomainPatternString =
            "^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$";

        public static bool IsValid(string address)
        {
            return !string.IsNullOrEmpty(address) &&
                (IPAddressValidation.IsValid(address) || Regex.IsMatch(address, DomainPatternString));
        }
    }
}
