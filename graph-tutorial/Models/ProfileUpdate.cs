using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace graph_tutorial.Models
{
    public class ProfileUpdate
    {
        public string MobilePhone { get; set; }
        public string OriginalMobilePhone { get; set; }
        public string PreferredName { get; set; }
        public string OriginalPreferredName { get; set; }
        public DateTime Birthday { get; set; }
        public DateTime OriginalBirthday { get; set; }
        public string MySite { get; set; }
        public string OriginalMySite { get; set; }
        public string AboutMe { get; set; }
        public string OriginalAboutMe { get; set; }
        public string[] Schools { get; set; }
        public string OriginalSchools { get; set; }
        public string[] Skills { get; set; }
        public string OriginalSkills { get; set; }
        public string[] Interests { get; set; }
        public string OriginalInterests { get; set; }

        public User GetUserForUpdate()
        {

            var updateUser = new User
            { 
                AboutMe = string.Compare(OriginalAboutMe, AboutMe) == 0 ? null : AboutMe,
                MySite = string.Compare(OriginalMySite, MySite) == 0 ? null : MySite,
                PreferredName = string.Compare(OriginalPreferredName, PreferredName) == 0 ? null : PreferredName,
                Interests = IsListModified(OriginalInterests, Interests) ? Interests : null,
                Schools = IsListModified(OriginalSchools, Schools) ? Schools : null,
                Skills = IsListModified(OriginalSkills, Skills) ? Skills : null,

                ODataType = null
            };

            if (DateTime.Compare(OriginalBirthday, Birthday) != 0)
            {
                updateUser.Birthday = Birthday;
            }

            if (updateUser.AboutMe == null &&
                updateUser.Birthday == null &&
                updateUser.MySite == null &&
                updateUser.PreferredName == null &&
                updateUser.Interests == null &&
                updateUser.Schools == null &&
                updateUser.Skills == null)
            {
                return null;
            }

            return updateUser;
        }

        // Currently you cannot update mobilePhone in the same request
        // as the other properties
        public User GetUserForMobilePhoneUpdate()
        {
            if (string.Compare(OriginalMobilePhone, MobilePhone) != 0)
            {
                return new User { MobilePhone = MobilePhone };
            }

            return null;
        }

        private bool IsListModified(string original, IEnumerable<string> updated)
        {
            var originalList = original.Split(';');

            if (originalList.Length != updated.Count())
                return true;

            // If the same length, there must be something in updated
            // that is not in original
            var changes = updated.Except(originalList);

            return changes.Count() > 0;
        }
    }
}