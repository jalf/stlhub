using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace STLHub.Services;

/// <summary>
/// Reads the operating system's "reduce motion" accessibility preference, so the app can drop
/// spatial movement for people who asked their system for less of it.
/// </summary>
public static class MotionPreference
{
    /// <summary>
    /// True when the system asks apps to reduce motion. Unknown platforms and failed lookups
    /// report false: animations are short and never carry information on their own.
    /// </summary>
    public static bool IsReducedMotionRequested()
    {
        try
        {
            if (OperatingSystem.IsWindows()) return !WindowsClientAreaAnimationEnabled();
            if (OperatingSystem.IsMacOS()) return MacReduceMotionEnabled();
            if (OperatingSystem.IsLinux()) return !GnomeAnimationsEnabled();
        }
        catch
        {
            // Preference lookup only; fall through to full motion.
        }
        return false;
    }

    // ── Windows: Settings › Accessibility › Visual effects › Animation effects ──

    private const uint SPI_GETCLIENTAREAANIMATION = 0x1042;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
        [MarshalAs(UnmanagedType.Bool)] ref bool pvParam, uint fWinIni);

    private static bool WindowsClientAreaAnimationEnabled()
    {
        var enabled = true;
        return !SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref enabled, 0) || enabled;
    }

    // ── macOS: System Settings › Accessibility › Display › Reduce motion ──

    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    [DllImport(ObjCLibrary, EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjCGetClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjCLibrary, EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern byte SendBool(IntPtr receiver, IntPtr selector);

    private static bool MacReduceMotionEnabled()
    {
        var workspaceClass = ObjCGetClass("NSWorkspace");
        if (workspaceClass == IntPtr.Zero) return false;
        var workspace = SendIntPtr(workspaceClass, SelRegisterName("sharedWorkspace"));
        if (workspace == IntPtr.Zero) return false;
        return SendBool(workspace, SelRegisterName("accessibilityDisplayShouldReduceMotion")) != 0;
    }

    // ── Linux (GNOME and derivatives): Accessibility › Reduce animation ──

    private static bool GnomeAnimationsEnabled()
    {
        using var process = Process.Start(new ProcessStartInfo("gsettings",
            "get org.gnome.desktop.interface enable-animations")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        if (process == null) return true;
        var output = process.StandardOutput.ReadToEnd().Trim();
        if (!process.WaitForExit(1000)) return true;
        return !string.Equals(output, "false", StringComparison.OrdinalIgnoreCase);
    }
}
