using System;
using System.Runtime.InteropServices;

namespace Acorda.Native;

// Mantém a janela "colada" no desktop: escondida da barra de tarefas/alt-tab e
// sempre no fundo da pilha Z, então só aparece quando o usuário minimiza tudo
// (Mostrar Área de Trabalho). Deliberadamente NÃO usa SetParent pra dentro do
// WorkerW do Explorer (truque clássico de wallpaper engine): isso tornaria a
// janela filha de um HWND que o Explorer recria/destrói periodicamente, e o
// Windows destrói janelas filhas junto com o pai — matando a janela de vez,
// sem chance de recuperação nem pelo tray. SetWindowPos(HWND_BOTTOM) evita
// esse risco por completo, ao custo de não ficar estritamente atrás dos
// ícones (fica no fundo do Z-order geral, o que visualmente já basta aqui).
public static class DesktopPinner
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;

    private static readonly IntPtr HWND_BOTTOM = new(1);
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr hWnd);

    // Ancora a janela no fundo do Z-order e some da taskbar/alt-tab.
    public static void Ancorar(IntPtr hwnd)
    {
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
    }

    // Reforça a posição no fundo do Z-order sem mexer no estilo (usado pelo watchdog:
    // outra janela pode ter roubado o fundo da pilha nesse meio tempo).
    public static void ReforcarFundo(IntPtr hwnd)
    {
        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
    }

    // Devolve a janela pro desktop normal (usado ao "abrir" o app).
    public static void Desancorar(IntPtr hwnd)
    {
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_APPWINDOW) & ~WS_EX_TOOLWINDOW);

        // Depois de trocar o ex-style é preciso forçar o SO a reavaliar frame/Z-order,
        // senão a janela pode ficar "presa" visualmente mesmo com taskbar/tray já
        // reconhecendo ela como aberta.
        SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
    }

    public static bool JanelaValida(IntPtr hwnd) => hwnd != IntPtr.Zero && IsWindow(hwnd);
}
