using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Acorda.Models;

namespace Acorda.ViewModels;

public class TarefaItemVM : INotifyPropertyChanged
{
    private bool _concluida;

    public Guid Id { get; }
    public string Texto { get; }
    public bool EhAgendada { get; }
    public string? DataLabel { get; }

    public bool Concluida
    {
        get => _concluida;
        set
        {
            if (_concluida == value) return;
            _concluida = value;
            OnPropertyChanged();
        }
    }

    public TarefaItemVM(TodoItem tarefa, bool concluidaHoje)
    {
        Id = tarefa.Id;
        Texto = tarefa.Texto;
        EhAgendada = tarefa.Tipo == TarefaTipo.Agendada;
        DataLabel = tarefa.DataAgendada?.ToString("dd/MM");
        _concluida = concluidaHoje;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? nome = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}
