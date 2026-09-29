using System;

namespace Acorda.Models;

// Registro histórico de conclusão de uma tarefa Fixa em um dia específico.
// Necessário porque a tarefa Fixa nunca é removida da lista ao ser concluída.
public class ConclusaoDiaria
{
    public Guid TarefaId { get; set; }
    public DateTime Data { get; set; } // apenas a data (sem hora), dia de referência
    public DateTime ConcluidaEm { get; set; }
}
