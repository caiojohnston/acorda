using System.Collections.Generic;

namespace Acorda.Models;

public class AppData
{
    public List<TodoItem> Tarefas { get; set; } = new();
    public List<ConclusaoDiaria> Conclusoes { get; set; } = new();
}
