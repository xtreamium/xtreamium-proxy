using Avalonia.Controls;
using Xtreamium.Tray.ViewModels;

namespace Xtreamium.Tray.Views;

public partial class AutostartChoiceWindow : Window {
  public AutostartChoiceWindow() {
    InitializeComponent();
    DataContextChanged += OnDataContextChanged;
  }

  private void OnDataContextChanged(object? sender, EventArgs e) {
    if (DataContext is AutostartChoiceViewModel vm) {
      vm.CloseRequested += (_, _) => Close();
    }
  }
}
