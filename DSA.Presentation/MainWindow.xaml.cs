public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        // Resolución manual del ViewModel desde el contenedor estático
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
    }
}