using System;
using System.Windows;

namespace Acorda;

public partial class EscolherDataWindow : Window
{
    public DateTime? DataSelecionada { get; private set; }

    public EscolherDataWindow()
    {
        InitializeComponent();
        CalendarioPicker.DisplayDateStart = DateTime.Today;
        CalendarioPicker.SelectedDate = DateTime.Today;
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        DataSelecionada = CalendarioPicker.SelectedDate ?? DateTime.Today;
        DialogResult = true;
    }
}
