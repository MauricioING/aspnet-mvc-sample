using graph_tutorial.Helpers;
using graph_tutorial.Models;
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
            ViewBag.ConsentUri = await GraphHelper.GetConsentUriForScopesIfNeeded(
                new string[] { "User.ReadWrite" },
                "/Profile");

            var userPhoto = await GraphHelper.GetUserPhotoAsDataUriAsync();
            ViewBag.FullSizePhoto = userPhoto;

            var userProfile = await GraphHelper.GetUserProfileAsync();

            return View(userProfile);
        }

        // POST: Profile/Update
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Update(ProfileUpdate profileUpdate)
        {
            var updateUser = profileUpdate.GetUserForUpdate();

            if (updateUser != null)
            {
                await GraphHelper.UpdateUserProfileAsync(updateUser);
            }

            var updatePhoneUser = profileUpdate.GetUserForMobilePhoneUpdate();
            if (updatePhoneUser != null)
            {
                await GraphHelper.UpdateUserProfileAsync(updatePhoneUser);
            }

            return RedirectToAction("Index");
        }
    }
}