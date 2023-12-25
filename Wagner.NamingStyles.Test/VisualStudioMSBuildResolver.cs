using System;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace Wagner.NamingStyles.Test
{
    /// <summary>
    /// Provides aid in selecting a version of MSBuild to register and then registers it using <see cref="MSBuildLocator.RegisterInstance"/>
    /// </summary>
    public static class VisualStudioMSBuildResolver
    {
        static VisualStudioMSBuildResolver()
        {
            //This is just to force Microsoft.CodeAnalysis.CSharp to be compiled with the project by utilizing a type from the assembly.
            var _ = CSharpFormattingOptions.IndentBlock;
        }
        /// <summary>
        /// Provides aid in selecting a version of MSBuild to register and then registers it using <see cref="MSBuildLocator.RegisterInstance"/>
        /// </summary>
        /// <param name="option">Selection method when multiple MSBuild instances exist.</param>
        /// <returns>True if a version of MSBuild was registered.</returns>
        public static bool Resolve(VisualStudioMSBuildResolverOption option = default)
        {
            // Attempt to set the version of MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances();

            if (!visualStudioInstances.Any())
            {
                Console.WriteLine($"No installed versions of MSBuild found.");
                return false;
            }

            if (option == VisualStudioMSBuildResolverOption.UseDefault)
            {
                Console.WriteLine($"Using default MSBuild instance to load projects.");
                MSBuildLocator.RegisterDefaults();
            }
            else
            {
                RegisterCustomVisualStudioInstance(visualStudioInstances.ToArray(), option);
            }
            if (MSBuildLocator.IsRegistered)
            {
                Console.WriteLine($"An instance of MSBuild was registered with {nameof(MSBuildLocator)}");
            }
            else
            {
                Console.WriteLine($"An instance of MSBuild could not registered with {nameof(MSBuildLocator)}");
            }
            return MSBuildLocator.IsRegistered;
        }

        private static void RegisterCustomVisualStudioInstance(VisualStudioInstance[] visualStudioInstances, VisualStudioMSBuildResolverOption option)
        {
            VisualStudioInstance instance = visualStudioInstances.Length == 1
                // If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstanceFromMultiple(visualStudioInstances, option);

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");


            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);

        }


        private static VisualStudioInstance SelectVisualStudioInstanceFromMultiple(VisualStudioInstance[] visualStudioInstances, VisualStudioMSBuildResolverOption option)
        {
            switch (option)
            {
                case VisualStudioMSBuildResolverOption.UseLatest:
                    return SelectLatestVisualStudioInstance(visualStudioInstances);
                case VisualStudioMSBuildResolverOption.Prompt:
                    return PromptSelectVisualStudioInstance(visualStudioInstances);
                default:
                    throw new ArgumentOutOfRangeException(nameof(option), $"Value of {nameof(option)} not expected.");
            }

        }

        private static VisualStudioInstance SelectLatestVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Version latestVersion = visualStudioInstances.Max(vs => vs.Version);
            return visualStudioInstances.First(vs => vs.Version == latestVersion);
        }

        private static VisualStudioInstance PromptSelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Console.WriteLine("Input not accepted, try again.");
            }
        }
    }


}
