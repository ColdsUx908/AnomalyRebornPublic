using Transoceanic.Core.Diagnostics;
using Transoceanic.Localization;
namespace Transoceanic.Commands;

/// <summary>
/// 开启或关闭调试模式的命令。
/// </summary>
internal sealed class DebugModeCommand : ModCommand, ILocalizationPrefix
{
    /// <inheritdoc/>
    /// <remarks>
    /// 该命令适用的调用环境，固定为聊天命令。
    /// </remarks>
    public override CommandType Type => CommandType.Chat;

    /// <inheritdoc/>
    /// <remarks>
    /// 聊天命令名称 debug，不包含前导斜杠。
    /// </remarks>
    public override string Command => "debug";

    /// <summary>
    /// 调试模式命令的本地化键前缀。
    /// </summary>
    public string LocalizationPrefix => TOLocalizationKeys.ModLocalizationPrefix + "Commands.DebugMode";

    /// <inheritdoc/>
    /// <remarks>
    /// 无参数时回复调试开关状态；on/true/1 启用，off/false/0 禁用，其他参数抛出 UsageException。
    /// </remarks>
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0)
        {
            caller.ReplyLocalizedTextFormat(this, "Status", Color.White, TODebug.DEBUG);
            return;
        }

        switch (args[0])
        {
            case "on":
            case "true":
            case "1":
                TODebug.DEBUG = true;
                caller.ReplyLocalizedText(this, "On");
                break;
            case "off":
            case "false":
            case "0":
                TODebug.DEBUG = false;
                caller.ReplyLocalizedText(this, "Off");
                break;
            default:
                throw new UsageException(Language.GetTextValue(TOLocalizationKeys.ModLocalizationPrefix + "Commands.DebugMode.InvalidArguments"));
        }
    }
}
