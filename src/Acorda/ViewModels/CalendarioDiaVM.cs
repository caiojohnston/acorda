using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Acorda.ViewModels;

public class CalendarioDiaVM : INotifyPropertyChanged
{
    private bool _selecionado;

    public DateTime Data { get; }
    public string NumeroDia { get; }
    public bool ForaDoMes { get; }
    public bool EhPassado { get; }
    public bool EhHoje { get; }

    public bool Selecionado
    {
        get => _selecionado;
        set
        {
            if (_selecionado == value) return;
            _selecionado = value;
            OnPropertyChanged();
        }
    }

    public bool Selecionavel => !ForaDoMes && !EhPassado;

    public CalendarioDiaVM(DateTime data, bool foraDoMes, DateTime hoje)
    {
        Data = data;
        NumeroDia = data.Day.ToString();
        ForaDoMes = foraDoMes;
        EhPassado = data.Date < hoje.Date;
        EhHoje = data.Date == hoje.Date;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? nome = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}
