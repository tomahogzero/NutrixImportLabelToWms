using System.Windows.Controls;
using NutrixSyncLabelToWms.App.ViewModels;

namespace NutrixSyncLabelToWms.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
        {
            vm.Settings.Epicor.Password = pb.Password;
        }
    }
}
