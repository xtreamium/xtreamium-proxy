using Avalonia.Controls;
using Xtreamium.Tray.ViewModels;

namespace Xtreamium.Tray.Views;

public partial class SettingsWindow : Window {
  public SettingsWindow() {
    InitializeComponent();
    DataContextChanged += OnDataContextChanged;
  }

  private void OnDataContextChanged(object? sender, EventArgs e) {
    if (DataContext is SettingsWindowViewModel vm) {
      vm.CloseRequested += (_, _) => Close();
    }
  }
}
