using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace PCRadio.Components.Controls;

public class ErrorData
{
    public string? Message { get; set; }
    public Icon? ActionIcon { get; set; }
    public string? ActionMessage { get; set; }
    public EventCallback<ToastResult>? ActionCallback { get; set; }
}
