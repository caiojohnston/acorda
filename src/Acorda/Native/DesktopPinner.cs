using System;
using System.Runtime.InteropServices;

namespace Acorda.Native;

// Ancora uma janela atrás dos ícones do desktop (dentro do WorkerW criado pelo Explorer).
// Truque clássico usado por wallpaper engines / gadgets: não é API oficial, pode falhar
// em builds futuras do Windows — por isso sempre com fallback em SetWindowPos(HWND_BOTTOM).
public static class DesktopPinner
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;

    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static IntPtr _workerW = IntPtr.Zero;

    private static IntPtr LocalizarWorkerW()
    {
        IntPtr progman = FindWindow("Progman", null!);
        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

        IntPtr workerw = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            IntPtr defView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null!);
            if (defView != IntPtr.Zero)
            {
                workerw = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null!);
            }
            return true;
        }, IntPtr.Zero);

        return workerw;
    }

    // Tenta anexar a janela atrás dos ícones do desktop. Retorna true se conseguiu via WorkerW.
    public static bool Ancorar(IntPtr hwnd)
    {
        // esconde da barra de tarefas / alt-tab
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

        _workerW = LocalizarWorkerW();
        if (_workerW != IntPtr.Zero)
        {
            SetParent(hwnd, _workerW);
            return true;
        }

        // fallback: mantém como janela normal, mas manda pro fundo da pilha Z
        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        return false;
    }

    // Desanexa do WorkerW e devolve a janela pro desktop normal (usado ao "abrir" o app).
    public static void Desancorar(IntPtr hwnd)
    {
        SetParent(hwnd, IntPtr.Zero);

        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_APPWINDOW) & ~WS_EX_TOOLWINDOW);
    }

    // Verifica se o Explorer reiniciou e o parent antigo não existe mais.
    public static bool ParentAindaValido()
    {
        return _workerW != IntPtr.Zero && IsWindow(_workerW);
    }
}
