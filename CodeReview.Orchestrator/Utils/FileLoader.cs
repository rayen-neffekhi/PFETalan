using System.IO;

namespace CodeReview.Orchestrator.Utils
{
    /// <summary>
    /// Small helper for loading files. Abstracted for easier unit testing.
    /// </summary>
    public class FileLoader
    {
        /// <summary>
        /// Load the text content of a file. Returns empty string if file not found.
        /// TODO: Expand to support reading from artifacts or remote storage if needed.
        /// </summary>
        public async Task<string> LoadTextAsync(string relativeOrAbsolutePath)
        {
            try
            {
                var path = relativeOrAbsolutePath ?? string.Empty;
                if (!Path.IsPathRooted(path))
                {
                    // Treat relative paths as workspace-relative (CI will set working directory appropriately).
                    path = Path.Combine(Directory.GetCurrentDirectory(), path);
                }

                if (!File.Exists(path))
                {
                    return string.Empty;
                }

                using var sr = new StreamReader(path);
                return await sr.ReadToEndAsync();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
