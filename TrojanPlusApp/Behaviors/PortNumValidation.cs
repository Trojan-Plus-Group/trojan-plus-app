using Xamarin.Forms;

namespace TrojanPlusApp.Behaviors
{
    public class PortNumValidation : Behavior<Entry>
    {
        public void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            ushort port;
            bool valid = ushort.TryParse(args.NewTextValue, out port) && port > 0;
            ((Entry)sender).TextColor = valid ? Color.Default : Color.Red;
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

    }
}
