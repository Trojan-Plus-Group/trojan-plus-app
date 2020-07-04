using System;
using System.ComponentModel;
using Xamarin.Forms;

using TrojanPlusApp.Models;
using TrojanPlusApp.ViewModels;

namespace TrojanPlusApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class HostEditPage : ContentPage
    {
        private readonly HostsViewModel hostsModel;
        private readonly bool addedOrEdit;

        public HostModel Item { get; set; }
        public bool ShowDeleteBtn { get; set; }
        public bool EnableEditHostName { get; set; }
        public string PageTitle
        {
            get
            {
                return addedOrEdit ? Resx.TextResource.New_AddHostTitle : Resx.TextResource.New_EditHostTitle;
            }
        }

        public HostEditPage(HostsViewModel viewModel, HostModel editHost)
        {
            InitializeComponent();

            hostsModel = viewModel;
            addedOrEdit = editHost == null;
            Item = editHost ?? new HostModel() { SSLVerify = true };
            ShowDeleteBtn = !addedOrEdit;
            EnableEditHostName = addedOrEdit;

            BindingContext = this;
        }

        public async void Save_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Item.HostName))
            {
                await DisplayAlert(
                    Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_HostErrorNeedName,
                    Resx.TextResource.Common_OK);
                return;
            }

            if (addedOrEdit)
            {
                if (hostsModel.DataStore.HasHost(Item.HostName))
                {
                    await DisplayAlert(
                        Resx.TextResource.Common_ErrorTitle,
                        string.Format(Resx.TextResource.New_HostHasBeenAdded, Item.HostName),
                        Resx.TextResource.Common_OK);

                    return;
                }
            }
            else
            {
                MessagingCenter.Send(this, "UpdateItem", Item);
                await Navigation.PopModalAsync();
                return;
            }

            MessagingCenter.Send(this, "AddItem", Item);
            await Navigation.PopModalAsync();
        }

        public async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        public async void OnDeleteBtnClicked(object sender, EventArgs e)
        {
            bool response = await DisplayAlert(
                Resx.TextResource.Common_AskTitle,
                Resx.TextResource.New_HostDeleteAskText,
                Resx.TextResource.Common_Yes,
                Resx.TextResource.Common_No);

            if (response)
            {
                MessagingCenter.Send(this, "DeleteItem", Item);
                await Navigation.PopModalAsync();
            }
        }

        public async void OnDuplicateBtnClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync(
                Resx.TextResource.Common_AskTitle,
                Resx.TextResource.New_AskDuplicatePrompt,
                Resx.TextResource.Common_OK,
                Resx.TextResource.Common_Cancel,
                initialValue: Item.HostName + " 1");

            if (name == null)
            {
                return;
            }

            if (hostsModel.DataStore.HasHost(name))
            {
                await DisplayAlert(
                    Resx.TextResource.Common_ErrorTitle,
                    string.Format(Resx.TextResource.New_HostHasBeenAdded, name),
                    Resx.TextResource.Common_OK);

                return;
            }

            MessagingCenter.Send(this, "AddItem", Item.Duplicate(name));
            await Navigation.PopModalAsync();
        }
    }
}