
using Codingstube.Modules;
using System.Text;
using System.Text.Json;

namespace Codingstube.Services {
    

    public class CommandFileService {

        public CommandFileService() { }

        public async Task<List<CustomCommand>> LoadCommandsFromFileAsync(string filePath, string fileName) {
            
            //construct full path
            string path = $"{filePath}/{fileName}";

            //create new list
            List<CustomCommand> commands = new();

            //check if file exists
            if (File.Exists(path)) {
                //read file
                string json = await File.ReadAllTextAsync(path);

                if (json.Length > 0) {
                    MemoryStream stream = new(Encoding.UTF8.GetBytes(json));

                    //deserialize json-string
                    var data = await JsonSerializer.DeserializeAsync<List<CustomCommand>>(stream);

                    if (data != null) {
                        //overwrite cmd list
                        commands = data;
                    }
                }
            } else {
                //file not found -> create file
                await File.WriteAllTextAsync(path, "");
            }

            return commands;
        }

        public async Task OverWriteCommandFileAsync(string filePath, string fileName, List<CustomCommand> commands) {
            
            //construct full path
            string path = $"{filePath}/{fileName}";

            //Convert List to json string
            string json = JsonSerializer.Serialize(commands, new JsonSerializerOptions() { WriteIndented = true });

            //overwrite command with new list
            await File.WriteAllTextAsync(path, json);
        }
    }
}
