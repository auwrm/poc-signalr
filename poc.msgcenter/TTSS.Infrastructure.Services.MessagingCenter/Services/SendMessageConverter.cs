using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TTSS.Infrastructure.Services.Models;

namespace TTSS.Infrastructure.Services
{
    public class SendMessageConverter : JsonConverter<IEnumerable<SendMessage<MessageContent>>>
    {
        public override IEnumerable<SendMessage<MessageContent>>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
            var result = new List<SendMessage<MessageContent>>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) return result;
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();

                var message = new SendMessage<MessageContent>();
                string? propertyName = reader.GetString();
                if (propertyName != nameof(message.Content)) throw new JsonException();

                reader.Read();
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

                var node = JsonNode.Parse(ref reader);
                if (!Enum.TryParse<MessageType>(getText(node, nameof(message.Content.Type)), out var type)) throw new JsonException();

                switch (type)
                {
                    case MessageType.Dynamic:
                        {
                            var content = new DynamicContent();
                            content.Data = getText(node, nameof(content.Data));
                            content.ContentType = getText(node, nameof(content.ContentType));
                            message.Content = content;
                            break;
                        }
                    case MessageType.Notification:
                        {
                            var content = new NotificationContent();
                            content.Message = getText(node, nameof(content.Message));
                            content.EndpointUrl = getText(node, nameof(content.EndpointUrl));
                            message.Content = content;
                            break;
                        }
                    default:
                        break;
                }

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        result.Add(message);
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        propertyName = reader.GetString();
                        reader.Read();
                        switch (propertyName)
                        {
                            case nameof(message.Nonce):
                                {
                                    message.Nonce = reader.GetString();
                                    break;
                                }
                            case nameof(message.TargetGroups):
                                {
                                    node = JsonNode.Parse(ref reader);
                                    var value = JsonSerializer.Deserialize<IEnumerable<string>>(node.ToString());
                                    message.TargetGroups = value;
                                    break;
                                }
                            case nameof(message.Filter):
                                {
                                    node = JsonNode.Parse(ref reader);
                                    var value = JsonSerializer.Deserialize<MessageFilter>(node.ToString());
                                    message.Filter = value;
                                    break;
                                }
                        }
                    }
                    else throw new JsonException();
                }
            }

            throw new JsonException();

            string getText(JsonNode? node, string propName)
                => node?[propName].ToString() ?? string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<SendMessage<MessageContent>> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
