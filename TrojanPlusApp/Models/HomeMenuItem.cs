using System;
using System.Collections.Generic;
using System.Text;

namespace TrojanPlusApp.Models
{
    public enum MenuItemType
    {
        AllHost,
        About
    }
    public class HomeMenuItem
    {
        public MenuItemType Id { get; set; }

        public string Title { get; set; }
    }
}
