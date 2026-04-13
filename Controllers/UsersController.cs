using Microsoft.AspNetCore.Mvc;

namespace SocialMediaAppBackend.Controllers;

public class UsersController
{

    [HttpGet("{id}")]
    public void Get(int id)
    {
        // do stuff
    }

}