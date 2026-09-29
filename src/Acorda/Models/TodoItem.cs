using System;

namespace Acorda.Models;

public class TodoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Texto { get; set; } = string.Empty;
    public TarefaTipo Tipo { get; set; } = TarefaTipo.Fixa;
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    // Posição na lista (arrastar-e-soltar). Índice fracionário: reordenar só toca no item movido.
    public double Ordem { get; set; }

    // Só usado quando Tipo == Agendada
    public DateTime? DataAgendada { get; set; }
    public bool Concluida { get; set; }
    public DateTime? DataConclusao { get; set; }
}
