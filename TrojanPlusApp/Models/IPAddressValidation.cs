using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace TrojanPlusApp.Models
{
    public class IPAddressValidation : Behavior<Entry>
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

        private static readonly string PatternString = "^([0-9]{1,3})\\.([0-9]{1,3})\\.([0-9]{1,3})\\.([0-9]{1,3})$";

        public static bool IsValid(string address)
        {
            return Regex.IsMatch(address, PatternString);
        }
    }
}
