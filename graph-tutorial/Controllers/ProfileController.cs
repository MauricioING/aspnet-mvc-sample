using graph_tutorial.Helpers;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace graph_tutorial.Controllers
{
    public class ProfileController : BaseController
    {
        // GET: Profile
        [Authorize]
        public async Task<ActionResult> Index()
        {
            var userPhoto = await GraphHelper.GetUserPhotoAsDataUriAsync();
            ViewBag.FullSizePhoto = userPhoto;

            var userProfile = await GraphHelper.GetUserProfileAsync();

            return View(userProfile);
        }

        // POST: Profile/Update
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Update(string workPhone, string mobilePhone, string streetAddress, string city, string state, string postalCode)
        {
            var updateUser = new User
            {
                BusinessPhones = string.IsNullOrEmpty(workPhone) ? null : new List<string> { workPhone },
                City = string.IsNullOrEmpty(city) ? null : city,
                MobilePhone = string.IsNullOrEmpty(mobilePhone) ? null : mobilePhone,
                PostalCode = string.IsNullOrEmpty(postalCode) ? null : postalCode,
                State = string.IsNullOrEmpty(state) ? null : state,
                StreetAddress = string.IsNullOrEmpty(streetAddress) ? null : streetAddress
            };

            await GraphHelper.UpdateUserProfileAsync(updateUser);

            return RedirectToAction("Index");
        }
    }
}