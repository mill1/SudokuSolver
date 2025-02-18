using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SudokuSolver.Api.Controllers;
using SudokuSolver.Api.Exceptions;
using SudokuSolver.Api.Interfaces;
using System.Net;

namespace SudokuSolverTests
{
    [TestClass]
    public class ControllerTests
    {
        [TestMethod]
        public void Get_ShouldReturnSucces200()
        {
            // Prepare
            var logger = new Mock<ILogger<SudokuController>>().Object;
            var service = new Mock<ISudokuService>();
            service.Setup(x => x.GetSudoku()).Returns("123");

            var controller = new SudokuController(logger, service.Object);

            // Act
            string result = controller.Get();

            // Assert
            Assert.AreEqual("123", result);
        }

        [TestMethod]
        public void Solve_ShouldReturnSucces200()
        {
            // Prepare
            var logger = new Mock<ILogger<SudokuController>>().Object;

            int[,] expected = new int[2, 2] { {1, 2}, {3, 4} };
            var service = new Mock<ISudokuService>();
            service.Setup(x => x.Solve(It.IsAny<string>())).Returns(expected);

            var controller = new SudokuController(logger, service.Object);

            // Act
            IActionResult result = controller.Solve("1  4");

            // Assert
            ((ObjectResult)result).StatusCode.Should().Be((int)HttpStatusCode.OK);
            ((ObjectResult)result).Value.Should().Be(expected);
        }

        [TestMethod]
        public void Solve_ShouldReturnBadRequest400()
        {
            // Prepare
            var logger = new Mock<ILogger<SudokuController>>().Object;
            
            var service = new Mock<ISudokuService>();
            service.Setup(x => x.Solve(It.IsAny<string>())).Throws(new InvalidPuzzleException("Expected 9x9 grid."));

            var controller = new SudokuController(logger, service.Object);

            // Act
            IActionResult result = controller.Solve("1  4");

            // Assert
            ((ObjectResult)result).StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            ((ObjectResult)result).Value.Should().Be("Invalid puzzle: Expected 9x9 grid.");
        }

        [TestMethod]
        public void Solve_ShouldReturnInternalServerError500()
        {
            // Prepare
            var logger = new Mock<ILogger<SudokuController>>().Object;

            var service = new Mock<ISudokuService>();
            service.Setup(x => x.Solve(It.IsAny<string>())).Throws(new DivideByZeroException());

            var controller = new SudokuController(logger, service.Object);

            // Act
            IActionResult result = controller.Solve("1  4");

            // Assert
            ((StatusCodeResult)result).StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        }
    }
}