using Microsoft.Extensions.AI;

namespace LocalInternalAIChatBot.Web.Models.DTO
{
    public class ChatMessageDTO
    {
        public ChatRole role { get; set; }
        public string content { get; set; } = ""; // used for streaming

        // Optional for alternate rendering
        public List<AIContent> Contents { get; set; } = new();

        public ChatMessageDTO(ChatRole paramrole, string paramcontent)
        {
            role = paramrole;
            content = paramcontent;
        }
    }

}
