// Based on Rainbow Folders 2 by Borodar (https://github.com/Borod4r/Rainbow-Folders-2), Apache License 2.0.
// Modified by NK Studio: ported to Unity 6 and refactored.

using System;
using System.Collections.Generic;
using UnityEngine;
using StorageHelper = NKStudio.FolderPainter.TexturesStorageHelper<NKStudio.FolderPainter.ProjectBackground>;

// Clr colors harmonized by NK Studio (OKLCH)

namespace NKStudio.FolderPainter
{
    public static class ProjectBackgroundsStorage
    {
        private static readonly Dictionary<ProjectBackground, Texture2D> BACKGROUND_TEXTURES = new Dictionary<ProjectBackground, Texture2D>();
        private static readonly Dictionary<ProjectBackground, Lazy<string>> BACKGROUND_STRINGS = new Dictionary<ProjectBackground, Lazy<string>>()
        {
            {ProjectBackground.ClrRed,         new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAd0lEQVR42q1S2w2AIBBrbxNncAH3H8QlhJwfanJiSg5ik34QjlwfcF+XDUANLDcPwee+AiiGC8Qbjhxo8ZB50C6y5KCECdnKwmfmNwU+mYG0gNEWspK91wJHt7cZZD4SVQuczADKApMZMFqYUeDRAgcr7YaYbYQnCvIkXiu22e4AAAAASUVORK5CYII=")},
            {ProjectBackground.ClrVermilion,   new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAeElEQVR42qWTUQ6AIAxDX7mCF/IS3t97CM4PNVlQDMwmZB9kazuK1mWagR3IQLnq5qo//q4AOXHCADEGA0iRJo97QM3erSY1pltHrwCNWnhYSg3GkAUFFOjPDsxbiChQ5Bn1tYPRIH0m0SJL7Pb9loOaNZSDyGfSAa0iIWpfpbp/AAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrOrange,      new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAeklEQVR42q2T0Q2AIAxEXzuF47iXy7qHaP3RBNBqQS8hTUq53F2KzNMwAhuQgPWoS1brk/JZ5R4S7MtJYLTDANOMWapL71GBXIF1WMBTQFCBaKNvV0E0yHrG1GG3oBppDZEnC7/uQThQ7fBdzGt0494ysOBn+hziRdkOebMllO9hsDoAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrAmber,       new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAeklEQVR42qWTUQ6AIAxDX3cKj+NtPZtnUMEPfyAwMqTJvmiXpis6j20HHiABdzGXMyUnGR9EjUwfDc8KgScaLjZnuwLLBMgmBF1Hxn/k0gHBDDRyoJUQo1do3Mz2oOGshDjsQTRE9woKhlhVefYv6E+Vs1ckdR4jGfAC86Any4TQLCEAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrYellow,      new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAfElEQVR42qWR0Q2AIAxEXzuTA7j/EI7gAhbrh34gEShyCSkE0t49ZN+WFTgBA9JTDTg+lmXVgKTccsYlAJofAioHiZYdO47KQa6Dtr3ca+UyGgllTlUG4UgaANeCKDMQXwy897AXQYIMpOXAZyJIb1LFjeQO/nxj1UE0jl9cFCUhcSUtYwAAAABJRU5ErkJggg==")},
            {ProjectBackground.ClrLime,        new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAeElEQVR42r2S0Q2AIAxEXxnEWRzcSVxFsH6oCWAaTz5sQiBpad9x2LJOM7ADGSjXnoEtWLmqLYkzHDAG4m6gXu7rvCZoEkED788pSJpIY7UEEwgsIvABguYNGHTBvrrgqguypRGBqzRJ/DCvNo7KCF34X4KLUx+UB9pHKNb4UBFUAAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrChartreuse,  new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAe0lEQVR42rVSQQ6AIAxrd/VTftK3+RjBejGRAJNF4pIFAqwra7ntywrgBJABpCKPTqZiTQCy4QlhHM0b613QB2C9N6eNnEpVbGhOUw6538eGyQgAcBbgdb60cKsAA/00g5iRgqo131TpRAbUcBlwrF3fnDNGUi3jFxV0AZu9JJeYZmVvAAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrHarlequin,   new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAhklEQVR42qWT6w0CMQyDP3cVFmEWtmUmKOYP3OUiqktLpKiqmocTu7rdL1egAy/g+fFHOLN/YzrQG7uZc1O6eyvgSvqPJlsBqVQgR6mFB00icETg4g4YjpAQqJjvxropI4hseGWEyEZ1oQcW+GeJXsx3ROBJWR+E5BPFjah2m+R+qEQt/kbeRMUncSVRWCgAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrGreen,       new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAcklEQVR42q2SUQ6AIAxDHzuCx/HLS3s60fqhH0gywsAmhCwZXemaln3dgAvIwPneGTick8te44GIQwAWfJTq2kamlrCCObUaHQXMKpD1TvIwY2KqCRT8gka20FQwEiTNEHwUqNN5tbagoKH84YEb5e4g3XjuICMcf/TTAAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrEmerald,     new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAdElEQVR42r2S0Q2AIAxEX9nBfVzGNV1M5PwxsWLQBhIvaSBpKde72rQuM7ADBchnbO704XMZKIkL4ht1jXwDowOJMVga+d0zUFCDZgPrYGG1Bj0MlAYeP0S0KO2WjQrshKr7bQQbdYG/N/HVRgVElHehdwwdnmYhW6oHjwEAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrSpringGreen, new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAh0lEQVR42qWSQRKDMAhFHxzJE/SIPZ9Xqfa7qAtMgxJlhknCJOEB36b5/QJW4Assu3/C2voSfHV+ZhytPffiBuAhoGRPFrfwgW4QSA2BjRLEEqo94KwHVB/1CFJsy0sQYH5Vqy6QfM+i3owLJg9ZKjooN7FKYKM6aO/IB+dXFlI2FnsqpD+wDZYFJ8JPogTGAAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrAquamarine,  new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAd0lEQVR42q2S0Q2AMAhEH93CbRzYcdzGWvzQJthYQ9FLGkLaHtyBTOsyAwXIwH7FzUR77F0GSuKEAkIAlcD7Wds8dR56CcVK0JdKT8TSShBHB24J6vWjJ4FRD0IjbDsYrm49+NxBdA/orbKOmhj1QX+dgjidv+EAGqwjvJAH5agAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrCyan,        new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAgUlEQVR42qWSSw7DIAxE3/g2vUXuv+u6R0mKu2gXDgqOoSNZSBiYD9bj+dqABryB41d7WGPF3gE04wvnDHGN/hx2cyGDALcq0wWRRwUsWpANml592AqsWQZYgZVM5WwG/b6P5sALCpQpoKhAcZC08I0+GyJZiPrHwhTryMJsiAL4AIOxJfnQyyaWAAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrSkyBlue,     new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAeElEQVR42q2S0Q2AMAhEH4zjFM7rRs5iK34Z20YaWr2EpKSX3HEgy7avwAlkIBV1OHX/ZyApD8x5l5CWo0UjDrELZQzWiuigojtCT6nnQDSgFMrAgg5ajo2GiDeCvCUcCLFyIDM2vmxh6pDwDumXNdrEGvFCDDu4APs6Jc0IF2NbAAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrAzure,       new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAg0lEQVR42qWTUQ4DIQhEH3ztsfZ+PWdP0V2nH22yVoPidhISjQgDA7Y9njtwAgU4KnsFVvsU5wMDxBzWnj14zECAvLqsomPQRs8w+AmgRDnKMri8bMjApk3UpBgPPmeb+pcKHQPdmYmoB7oTYFUNa1UYZv2qYdEo47m6FTIo8dYNl+kNLPYpm0HKqkAAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrCerulean,    new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAd0lEQVR42qWT6wnAIAyEvzhAZ+o8XbxDWJv+KAUfaE08CAh5cJecsh3nDiTgBq4sYifymhR4IYDyD6neEjrJWeg3QD3NAFYJDawStK4PneTMEgsJ6mBAfoXZC8hIAkYGjQQrg8IH4mDAiIF6fMDKX/A6UVecCCAPlyMkg4N0JMQAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrBlue,        new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAeUlEQVR42qVSyQ3AMAizWakLdIfuv0IX6OG+qiKURIRa4hUDtgmXbV8BXABuAKero1OecxvaEJJ4BzDJjzzZt1EjYkYBlbCguMQqW32PTQbXzSA+Mnsdq5yuNUBhzvQ/cLI5ssBRiLOQD5GFDBgzYEHBLwuIFlCx8AD8BiSZWJp6wQAAAABJRU5ErkJggg==")},
            {ProjectBackground.ClrIndigo,      new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAeklEQVR42q2SSw6AIAxEXzmZW0/h/VceQMi40EXBHzROMiEpIRmmz5Z5nYDinE9vN87uzEBJHDL6pHaQni46ZIClZtDzqEqUiEttAgU6kC9x9Av2lkAdCeQT6KOszwQRDuQ5INjB8Bp/JVFtBwqgXG1htIMLiQxSaQA7jg8ou2Rs5tsAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrViolet,      new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAdElEQVR42q2SQQ6AIAwEZ/syz77Ph3pXrAc9VA2miE0aIKWhO6ymcR6AAmzAeuYS1pixVoBiHCHawwEsHrJNMazxVd33NQlKTOBvE6Ql9Uh4MPA/ISoJUVYpei9EJXm4fSAf76v3Gy9WViPEVx94koF6rew7PCgm8ASwt3MAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrPurple,      new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAfklEQVR42qWS0Q3DMAhEH54sG3TjDNQV6uT6kSpCrkCFIiGEbQ64s+2P5wacwASOT3y5uPp0fg4uE2DUzACGS7ReRkWu6Q1AYwI8gJZzBe8VAVhSlHGg6gqWrfALiYpIbBEI2MgkqqjQtlHsuK6qqozKJrCGpPbPR2qR+MXBG3oWJRwTlmFxAAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrMagenta,     new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAhElEQVR42qWTUQ7DIAxDn3OyHqD3/+jfzrG26ccmLaIEhc5ShBBgbAf0WrcFOIAT2L/1DmOsuLYDp/GBAOcH0Ye3c0sWMrTEsuJNKSxhr1hQtMADCx4VaEZ2z0L1sGcEKoaoQYj+JMRIoLEC9cms3Hvvt3q2jT5SQCGDdo/+/QvTL/GGC8xfJyrxkKReAAAAAElFTkSuQmCC")},
            {ProjectBackground.ClrFuchsia,     new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAfUlEQVR42qWTUQrDMAxDn3yynmBn3bF2h7bTPrZCmtbFXQQm4AgbSYlej+cErMAbWJqak9ruV2AJvhB7mHP0PEWRmMHBILIBLvR9NYCCNPUeeMQDj3qg4tbeg1QC1XgzCS6m42h6Kph44MTv1FnGd9+BRlLwH39BfYx3YYAPuVMnCONt6VQAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrRose,        new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAg0lEQVR42qWS0Q2AIAxEXzuWX27mvM4gWD9MFBSwyCUNbRPK9Q5Zp2UGIrADIYmtECE5AxCVEwIYXNWd1yEA+mwAYEn+Ab2u5PAwMED09Xq5rq1g2pju6mtLIE9/hIG1RMTBILNROpQvutCj/JCIVrPxz0d6MbDOdXiKKA4Gbg3wOnIAw5QnxNzcLpAAAAAASUVORK5CYII=")},
            {ProjectBackground.ClrCrimson,     new Lazy<string>(()=> "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAgElEQVR42qWTyw2AMAxDX7oTd2Zg/xWYgI85IAQUggxYilS1aRrbafRN1wITMAPjIYYktvMJGAsrgjPEPeq8KGZiBhV+IisgY19PBTCoRa2BvmkQjy0bGmhvx3mxWqcUcO0tyWWZ7pzmIAwRLznHUda/OQjbukQDvf4LFwpvIYAFKD0nln1Wy6YAAAAASUVORK5CYII=")},
        };

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static Texture2D GetBackground(ProjectBackground type)
        {
            return StorageHelper.GetTexture(type, FilterMode.Bilinear, BACKGROUND_STRINGS, BACKGROUND_TEXTURES);
        }
    }
}