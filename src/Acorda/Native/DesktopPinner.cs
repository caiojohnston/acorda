using System;
using System.Runtime.InteropServices;

namespace Acorda.Native;

// Mantém a janela "colada" no desktop: escondida da barra de tarefas/alt-tab e
// posicionada no Z-order logo atrás da janela que hospeda os ícones do desktop
// (SHELLDLL_DefView) — na frente do papel de parede, atrás dos ícones. Deliberadamente
// NÃO usa SetParent pra dentro do WorkerW do Explorer (truque clássico de wallpaper
// engine): isso tornaria a janela filha de um HWND que o Explorer recria/destrói
// periodicamente, e o Windows destrói janelas filhas junto com o pai — matando a
// janela de vez, sem chance de recuperação nem pelo tray. Usar SetWindowPos com
// hWndInsertAfter apontando pra janela dos ícones consegue o mesmo efeito visual
// sem nunca criar essa relação de pai/filho.
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
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // Acha a janela de topo que hospeda os ícones do desktop (SHELLDLL_DefView),
    // seja ela o Progman direto (Windows mais antigo) ou um WorkerW criado pelo
    // Explorer depois do truque de mensagem abaixo (Windows 10/11).
    private static IntPtr LocalizarJanelaDosIcones()
    {
        IntPtr progman = FindWindow("Progman", null!);
        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

        IntPtr janelaComIcones = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            if (FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null!) != IntPtr.Zero)
            {
                janelaComIcones = hwnd;
                return false; // achou, para de enumerar
            }
            return true;
        }, IntPtr.Zero);

        return janelaComIcones != IntPtr.Zero ? janelaComIcones : progman;
    }

    private static void PosicionarAtrasDosIcones(IntPtr hwnd)
    {
        var janelaIcones = LocalizarJanelaDosIcones();
        var insertAfter = janelaIcones != IntPtr.Zero ? janelaIcones : HWND_BOTTOM;
        SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
    }

    // Ancora a janela atrás dos ícones do desktop e some da taskbar/alt-tab.
    public static void Ancorar(IntPtr hwnd)
    {
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        PosicionarAtrasDosIcones(hwnd);
    }

    // Reforça a posição atrás dos ícones (usado pelo watchdog: o Explorer pode ter
    // recriado a janela dos ícones, ou outra janela pode ter roubado a posição).
    public static void ReforcarFundo(IntPtr hwnd) => PosicionarAtrasDosIcones(hwnd);

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
