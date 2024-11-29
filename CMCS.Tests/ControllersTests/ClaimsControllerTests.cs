using CMCS.Controllers;
using CMCS.Data;
using CMCS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMCS.Tests.ControllersTests
{
    [TestClass]
    public class ClaimsControllerTests
    {
        private ApplicationDbContext _context;
        private ClaimsController _controller;

        [TestInitialize]
        public void Setup()
        {
            // Initialize an in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new ApplicationDbContext(options);

            // Clear existing data to avoid key duplication issues
            _context.Claims.RemoveRange(_context.Claims);
            _context.Documents.RemoveRange(_context.Documents);
            _context.SaveChanges();

            // Initialize the controller with the in-memory context
            _controller = new ClaimsController(_context);
        }

        [TestMethod]
        public async Task SubmitClaim_ValidClaim_RedirectsToViewClaims()
        {
            // Arrange
            var claim = new MonthlyClaim
            {
                LecturerName = "Jane Doe",
                HoursWorked = 75,
                HourlyRate = 50,
                Notes = "Important project claim",
                Status = "Pending"
            };

            // Mock file upload
            var fileName = "testdocument.pdf";
            var fileContent = "Sample file content for testing";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(fileContent);
            writer.Flush();
            stream.Position = 0;

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.ContentDisposition).Returns($"inline; filename={fileName}");

            // Act
            var result = await _controller.SubmitClaim(claim, mockFile.Object) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ViewClaims", result.ActionName);
            Assert.AreEqual(1, _context.Claims.Count());
            Assert.AreEqual(fileName, _context.Documents.First().FileName);
        }

        [TestMethod]
        public async Task SubmitClaim_WithInvalidModel_ReturnsViewWithErrors()
        {
            // Arrange
            var claim = new MonthlyClaim(); // Missing required fields
            _controller.ModelState.AddModelError("LecturerName", "Lecturer Name is required.");

            // Act
            var result = await _controller.SubmitClaim(claim, null) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey("LecturerName"));
        }

        [TestMethod]
        public async Task UpdateClaimStatus_ApprovesClaimSuccessfully()
        {
            // Arrange
            var claim = new MonthlyClaim
            {
                Id = 1,
                LecturerName = "John Doe",
                HoursWorked = 10,
                HourlyRate = 50,
                Notes = "Test Note",
                Status = "Pending"
            };
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            // Act
            await _controller.UpdateClaimStatus(claim.Id, "Approved", "Good work!");

            // Assert
            var updatedClaim = _context.Claims.FirstOrDefault(c => c.Id == claim.Id);
            Assert.IsNotNull(updatedClaim);
            Assert.AreEqual("Approved", updatedClaim.Status);
            Assert.AreEqual("Good work!", updatedClaim.Notes);
        }
    }
}