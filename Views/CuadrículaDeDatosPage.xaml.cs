using DSAapp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace DSAapp.Views;

// TODO: Change the grid as appropriate for your app. Adjust the column definitions on DataGridPage.xaml.
// For more details, see the documentation at https://docs.microsoft.com/windows/communitytoolkit/controls/datagrid.
public sealed partial class CuadrículaDeDatosPage : Page
{
    public CuadrículaDeDatosViewModel ViewModel
    {
        get;
    }

    public CuadrículaDeDatosPage()
    {
        ViewModel = App.GetService<CuadrículaDeDatosViewModel>();
        InitializeComponent();
    }
}
