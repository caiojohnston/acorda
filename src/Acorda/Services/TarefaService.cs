using System;
using System.Collections.Generic;
using System.Linq;
using Acorda.Models;

namespace Acorda.Services;

public class TarefaService
{
    private readonly StorageService _storage;
    private AppData _dados;

    public TarefaService(StorageService storage)
    {
        _storage = storage;
        _dados = _storage.CarregarDados();
    }

    private void Salvar() => _storage.SalvarDados(_dados);

    public TodoItem AdicionarFixa(string texto)
    {
        var tarefa = new TodoItem { Texto = texto, Tipo = TarefaTipo.Fixa };
        _dados.Tarefas.Add(tarefa);
        Salvar();
        return tarefa;
    }

    public TodoItem AdicionarAgendada(string texto, DateTime dataAgendada)
    {
        var tarefa = new TodoItem
        {
            Texto = texto,
            Tipo = TarefaTipo.Agendada,
            DataAgendada = dataAgendada.Date
        };
        _dados.Tarefas.Add(tarefa);
        Salvar();
        return tarefa;
    }

    public void Excluir(Guid tarefaId)
    {
        _dados.Tarefas.RemoveAll(t => t.Id == tarefaId);
        _dados.Conclusoes.RemoveAll(c => c.TarefaId == tarefaId);
        Salvar();
    }

    public void Adiar(Guid tarefaId, DateTime novaData)
    {
        var tarefa = _dados.Tarefas.FirstOrDefault(t => t.Id == tarefaId);
        if (tarefa is null || tarefa.Tipo != TarefaTipo.Agendada) return;
        tarefa.DataAgendada = novaData.Date;
        Salvar();
    }

    // Retorna as tarefas visíveis hoje: todas as Fixas + Agendadas cuja data já chegou.
    public List<TodoItem> ListarVisiveisHoje()
    {
        var hoje = DateTime.Today;
        return _dados.Tarefas
            .Where(t => t.Tipo == TarefaTipo.Fixa || (t.DataAgendada is not null && t.DataAgendada.Value.Date <= hoje))
            .OrderBy(t => t.Tipo == TarefaTipo.Agendada && t.Concluida)
            .ThenBy(t => t.DataCriacao)
            .ToList();
    }

    public bool EstaConcluidaHoje(TodoItem tarefa)
    {
        if (tarefa.Tipo == TarefaTipo.Agendada)
            return tarefa.Concluida;

        var hoje = DateTime.Today;
        return _dados.Conclusoes.Any(c => c.TarefaId == tarefa.Id && c.Data.Date == hoje);
    }

    public void AlternarConclusao(Guid tarefaId)
    {
        var tarefa = _dados.Tarefas.FirstOrDefault(t => t.Id == tarefaId);
        if (tarefa is null) return;

        if (tarefa.Tipo == TarefaTipo.Agendada)
        {
            tarefa.Concluida = !tarefa.Concluida;
            tarefa.DataConclusao = tarefa.Concluida ? DateTime.Now : null;
        }
        else
        {
            var hoje = DateTime.Today;
            var existente = _dados.Conclusoes.FirstOrDefault(c => c.TarefaId == tarefaId && c.Data.Date == hoje);
            if (existente is not null)
            {
                _dados.Conclusoes.Remove(existente);
            }
            else
            {
                _dados.Conclusoes.Add(new ConclusaoDiaria { TarefaId = tarefaId, Data = hoje, ConcluidaEm = DateTime.Now });
            }
        }

        Salvar();
    }

    // Quantidade de tarefas concluídas por dia, últimos 7 dias (hoje incluso).
    public Dictionary<DateTime, int> EstatisticaSemanal()
    {
        var resultado = new Dictionary<DateTime, int>();
        var hoje = DateTime.Today;

        for (int i = 6; i >= 0; i--)
        {
            var dia = hoje.AddDays(-i);
            var fixasNoDay = _dados.Conclusoes.Count(c => c.Data.Date == dia);
            var agendadasNoDia = _dados.Tarefas.Count(t =>
                t.Tipo == TarefaTipo.Agendada && t.Concluida && t.DataConclusao?.Date == dia);

            resultado[dia] = fixasNoDay + agendadasNoDia;
        }

        return resultado;
    }
}
