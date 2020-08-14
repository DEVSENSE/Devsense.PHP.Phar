using System;
using Xunit;

namespace Devsense.PHP.Phar.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void PhpUnitFiles()
        {
            var phar = PharFile.OpenPharFile("phpunit.phar");
            var manifest = phar.Manifest;

            Assert.True(manifest.Entries.TryGetValue("php-timer/Duration.php", out var entry) && entry.IsFile);
        }
    }
}
