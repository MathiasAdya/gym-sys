using System.Windows;
using System.Windows.Controls;

namespace GymSys.App.Views;

public partial class MemberFormView : UserControl
{
    public MemberFormView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => NamaBox.Focus();
}
