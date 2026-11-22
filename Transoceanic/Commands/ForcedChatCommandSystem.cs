using Terraria.Chat;
using Terraria.Chat.Commands;
using Terraria.UI.Chat;
using Transoceanic.Localization;

namespace Transoceanic.Commands;

internal sealed class ForcedChatCommandSystem : ModSystem
{
    public override void Load()
    {
        MethodInfo handler = typeof(CommandLoader).GetMethod("HandleCommand", BindingFlags.Static | BindingFlags.NonPublic,
            [typeof(string), typeof(CommandCaller)]) ?? throw new MissingMethodException(typeof(CommandLoader).FullName, "HandleCommand");
        MonoModHooks.Add(handler, HandleCommand);
    }

    private static bool HandleCommand(Func<string, CommandCaller, bool> orig, string input, CommandCaller caller)
    {
        if (caller.CommandType is not (CommandType.Chat or CommandType.Server) || !input.StartsWith("//", StringComparison.Ordinal))
            return orig(input, caller);

        string command = input[1..];
        if (orig(command, caller))
            return true;

        string name = command[1..].Split(' ', 2)[0];
        if (caller.CommandType == CommandType.Server)
            return ReplyUnknown();

        ChatMessage message = ChatManager.Commands.CreateOutgoingMessage(command);
        if (message.IsConsumed)
            return true;

        if (!message.CommandId.Equals(ChatCommandId.FromType<SayChatCommand>()))
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                ChatHelper.SendChatMessageFromClient(message);
            else
                ChatManager.Commands.ProcessIncomingMessage(message, Main.myPlayer);
            return true;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient && TOMain.Instance.IsNetSynced)
        {
            // Keep the forced prefix so the server can resolve server-only commands or reply privately.
            ChatHelper.SendChatMessageFromClient(new ChatMessage(input));
            return true;
        }

        return ReplyUnknown();

        bool ReplyUnknown()
        {
            caller.Reply(Language.GetTextValue(TOLocalizationKeys.ModLocalizationPrefix + "Commands.UnknownCommand", "//" + name), Color.Red);
            return true;
        }
    }
}
