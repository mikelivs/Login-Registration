using MVC_LoginRegister.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace MVC_LoginRegister.Controllers
{
    public class UserController : Controller
    {
        //user controller
      //Registration Action
      [HttpGet]
      public ActionResult Registration()
        {
            return View();
        }

        //Registration POST action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified, ActivationCode")] User user)
        {
            //initialize variables from registration page
            Boolean Status = false;
            string message = "";
            try
            {
                //Model Validation
                if (ModelState.IsValid)
                {
                    #region
                    //Email already exists
                    var isExists = IsEmailExist(user.EmailID); //method call

                    if (isExists)
                    {
                        ModelState.AddModelError("EmailExist", "Email already exists");
                    }
                    #endregion

                    #region Generate Activation Code
                    user.ActivationCode = Guid.NewGuid();
                    #endregion

                    #region Password Hashing
                    user.Password = Crypto.Hash(user.Password);
                    user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword); //
                    #endregion

                    user.IsEmailVerified = false;

                    #region Save to Database
                    using (myDBEntities dc = new myDBEntities())
                    {

                        dc.Users.Add(user);
                        dc.SaveChanges(); //bug when saving data. 

                        //send email
                        SendVerificationLinkEmail(user.EmailID, user.ActivationCode.ToString());
                        message = "Registration successfull, Email has been sent to your email: " + user.EmailID;
                        Status = true;


                    }
                    #endregion
                }

                else
                {
                    message = "Invalid request";
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("eror" + e);
            }

            //Email Exists

            //Generate activation code



            //save data to database


            ViewBag.Message = message;
                ViewBag.Status = Status;
                return View(user);

            }
                
            



        //Verify account
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;

            using (myDBEntities dc = new myDBEntities())
            {
                dc.Configuration.ValidateOnSaveEnabled = false; //avoid confrim password doesnt match issue

                var v = dc.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if( v != null)
                {
                    v.IsEmailVerified = true;
                    dc.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "invalid request";
                }
                
            }
            ViewBag.Status = Status;  

                return View();
        }

        //Login

        //Login POST

        //Logout

        //Methods
        [NonAction]
        public Boolean IsEmailExist(string emailID)
        {
            using(myDBEntities dc = new myDBEntities()) //call DB
                {               
                var v = dc.Users.Where(a => a.EmailID == emailID).FirstOrDefault(); //Searches database "WHERE" emailID and stores in var
                return v != null; //true 
            }
        }

        [NonAction]
        public void SendVerificationLinkEmail(string emailID, string activationCode)
        {
            var scheme = Request.Url.Scheme;
            var host = Request.Url.Host;
            var port = Request.Url.Port;

            string url = scheme + "://" + host;

            var verifyUrl = "/User/VerifyAccount/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("mikelivs32@gmail.com", "Register");
            var toEmail = new MailAddress(emailID);
            var fromEmailPassword = "Tgif2day";
            var subject = "Your account confirmation";

            string body = "We are excited to tell you that your .NET account has been created"+
                "<a href= '"+link+"'>"+link+"</a>";


            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl =true,
                DeliveryMethod =  SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(fromEmail.Address,fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true

            })
                smtp.Send(message);

        }

    }

}
