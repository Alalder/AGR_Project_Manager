using System.Collections.Generic;
using System.Linq;
using AGR_Project_Manager.Models;

namespace AGR_Project_Manager.Services
{
    public class NameGeneratorService
    {
        private bool NeedsModelSuffix(Project project)
        {
            if (project == null) return false;
            int modelCount = project.Models.Count(m =>
                !m.Name.Equals("Ground", System.StringComparison.OrdinalIgnoreCase));
            return modelCount > 1;
        }

        private List<string> GetModelNumbers(Project project)
        {
            if (project == null) return new List<string> { "001" };

            return project.Models
                .Where(m => !m.Name.Equals("Ground", System.StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Name)
                .ToList();
        }

        #region Geometry

        public List<string> GenerateGeometryHighPoly(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            bool needsSuffix = NeedsModelSuffix(project);
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                string suffix = needsSuffix ? $"_{model}" : "";
                result.Add($"SM_{addr}{suffix}_Main");
                result.Add($"SM_{addr}{suffix}_MainGlass");
            }

            result.Add($"SM_{addr}_Ground");
            result.Add($"SM_{addr}_GroundGlass");

            return result;
        }

        public List<string> GenerateGeometryLowPoly(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                result.Add($"SM_{addr}_{model}_Main");
                result.Add($"SM_{addr}_{model}_MainGlass");
            }

            result.Add($"SM_{addr}_Ground");
            result.Add($"SM_{addr}_GroundGlass");
            result.Add($"SM_{addr}_GroundEl");
            result.Add($"SM_{addr}_Flora");

            return result;
        }

        #endregion

        #region Materials

        public List<string> GenerateMaterialsHighPoly(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            bool needsSuffix = NeedsModelSuffix(project);
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                string suffix = needsSuffix ? $"_{model}" : "";
                result.Add($"M_{addr}{suffix}_Main_1");
                result.Add($"M_{addr}{suffix}_MainGlass_1");
            }

            result.Add($"M_{addr}_Ground_1");
            result.Add($"M_{addr}_GroundGlass_1");

            return result;
        }

        public List<string> GenerateMaterialsLowPoly(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                result.Add($"M_{addr}_{model}_Main_1");
            }

            result.Add("M_Glass_01");
            result.Add($"M_{addr}_Ground_1");
            result.Add($"M_{addr}_GroundGlass_1");
            result.Add($"M_{addr}_GroundEl_1");
            result.Add($"M_{addr}_Flora_1");

            return result;
        }

        #endregion

        #region Textures

        public List<string> GenerateTexturesHighPoly(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            bool needsSuffix = NeedsModelSuffix(project);
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                string suffix = needsSuffix ? $"_{model}" : "";
                result.Add($"T_{addr}{suffix}_Diffuse_1.1001");
                result.Add($"T_{addr}{suffix}_ERM_1.1001");
                result.Add($"T_{addr}{suffix}_Normal_1.1001");
            }

            return result;
        }

        public List<string> GenerateTexturesLowPoly(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            bool needsSuffix = NeedsModelSuffix(project);
            var models = GetModelNumbers(project);
            string[] suffixes = { "d", "r", "m", "n", "o" };

            foreach (var model in models)
            {
                string modelSuffix = needsSuffix ? $"_{model}" : "";
                foreach (var s in suffixes)
                {
                    result.Add($"T_{addr}{modelSuffix}_Main_{s}_1");
                }
            }

            foreach (var s in suffixes)
            {
                result.Add($"T_{addr}_Ground_{s}_1");
            }

            result.Add($"T_{addr}_GroundEl_d_1");
            result.Add($"T_{addr}_GroundEl_o_1");
            result.Add($"T_{addr}_Flora_d_1");
            result.Add($"T_{addr}_Flora_o_1");

            return result;
        }

        #endregion

        #region Collisions and Lighting

        public List<string> GenerateCollisions(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            bool needsSuffix = NeedsModelSuffix(project);
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                string suffix = needsSuffix ? $"_{model}" : "";
                result.Add($"UCX_SM_{addr}{suffix}_Main_001");
            }

            result.Add($"UCX_SM_{addr}_Ground_001");

            return result;
        }

        public List<string> GenerateLighting(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            bool needsSuffix = NeedsModelSuffix(project);
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                string suffix = needsSuffix ? $"_{model}" : "";
                result.Add($"{addr}{suffix}_Root");
                result.Add($"{addr}{suffix}_Omni_001");
                result.Add($"{addr}{suffix}_Spot_001");
            }

            result.Add($"{addr}_Ground_Root");
            result.Add($"{addr}_Ground_Omni_001");
            result.Add($"{addr}_Ground_Spot_001");

            return result;
        }

        #endregion

        #region FBX and Archives

        public List<string> GenerateFbxHighPoly(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            bool needsSuffix = NeedsModelSuffix(project);
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                string suffix = needsSuffix ? $"_{model}" : "";
                result.Add($"SM_{addr}{suffix}");
            }

            result.Add($"SM_{addr}_Ground");

            foreach (var model in models)
            {
                string suffix = needsSuffix ? $"_{model}" : "";
                result.Add($"SM_{addr}{suffix}_Light");
            }

            result.Add($"SM_{addr}_Ground_Light");

            return result;
        }

        public List<string> GenerateFbxLowPoly(Project project, string districtCode)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;

            // Если код не задан - показываем XXXX
            string district = string.IsNullOrEmpty(districtCode) ? "XXXX" : districtCode;

            var models = GetModelNumbers(project);

            int index = 1;
            foreach (var model in models)
            {
                result.Add($"{district}_{addr}_{index:D2}");
                index++;
            }

            result.Add($"{district}_{addr}_Ground");

            return result;
        }

        public List<string> GenerateArchivesHighPoly(Project project)
        {
            if (project == null) return new List<string>();

            var result = new List<string>();
            string addr = project.Name;
            bool needsSuffix = NeedsModelSuffix(project);
            var models = GetModelNumbers(project);

            foreach (var model in models)
            {
                string suffix = needsSuffix ? $"_{model}" : "";
                result.Add($"SM_{addr}{suffix}");
            }

            result.Add($"SM_{addr}_Ground");

            return result;
        }

        public List<string> GenerateArchiveLowPoly(Project project, string districtCode)
        {
            if (project == null) return new List<string>();

            string addr = project.Name;

            // Если код не задан - показываем XXXX
            string district = string.IsNullOrEmpty(districtCode) ? "XXXX" : districtCode;

            return new List<string> { $"{district}_{addr}" };
        }

        #endregion
    }
}