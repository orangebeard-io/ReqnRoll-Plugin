using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Reqnroll;

namespace Orangebeard.ReqnrollPlugin
{
    internal class FeatureInfoEqualityComparer : IEqualityComparer<FeatureInfo>
    {
        public bool Equals(FeatureInfo x, FeatureInfo y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            return x?.Title == y?.Title
                   && x?.Description == y?.Description
                   && x?.GenerationTargetLanguage == y?.GenerationTargetLanguage
                   && x.Tags.SequenceEqual(y.Tags);
        }

        public int GetHashCode(FeatureInfo obj)
        {
            return GetFeatureInfoHashCode(obj);
        }

        public static int GetFeatureInfoHashCode(FeatureInfo obj)
        {
            return obj.Title.GetHashCode()
                   ^ (obj.Description ?? string.Empty).GetHashCode()
                   ^ obj.GenerationTargetLanguage.GetHashCode()
                   ^ obj.Language.DisplayName.GetHashCode()
                   ^ ((IStructuralEquatable)obj.Tags).GetHashCode(EqualityComparer<string>.Default);
        }
    }
}