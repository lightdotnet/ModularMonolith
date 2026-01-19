using MudBlazor;

namespace Monolith.Blazor.Shared;

/// <summary>
/// Represents the theme configuration for the application.
/// </summary>
public class Theme
{
    private const string primaryColor = "#00b14f";

    /// <summary>
    /// Gets the application theme.
    /// </summary>
    /// <returns>The application theme.</returns>
    public static MudTheme ApplicationTheme()
    {
        var theme = new MudTheme()
        {
            PaletteLight = new()
            {
                // **Primary Colors**
                Primary = primaryColor, // Adjusted, used for highlights and key elements
                Secondary = "#9E9E9E", // Dark gray, secondary color

                // **Background and Surface**
                Background = "#FFF", // Standard light background color
                Surface = "#FFFFFF", // Light gray, used for surfaces like cards

                // **Text Colors**
                TextPrimary = "#424242", // Dark gray, primary text color
                TextSecondary = "#6D6D6D", // Medium gray, secondary text color
                TextDisabled = "rgba(0,0,0,0.38)", // Semi-transparent black, disabled text color

                // **Accent Colors**
                Success = "#4CAF50", // Green, used for success messages
                Warning = "#FF9800", // Orange, used for warning messages
                Error = "#F44336", // Red, used for error messages
                Info = "#2196F3", // Blue, used for informational messages

                // **Contrast Text for Accent Colors**
                SuccessContrastText = "#FFFFFF", // White, text color for success messages
                WarningContrastText = "#FFFFFF", // White, text color for warning messages
                ErrorContrastText = "#FFFFFF", // White, text color for error messages
                InfoContrastText = "#FFFFFF", // White, text color for informational messages

                // **Dividers and Borders**
                Divider = "rgba(0,0,0,0.12)", // Semi-transparent black, used for dividers

                // **Hover and Ripple Effects**
                HoverOpacity = 0.04, // Opacity for hover effects
                RippleOpacity = 0.08, // Opacity for ripple effects

                // **Overlay**
                OverlayLight = "rgba(255,255,255,0.5)", // Semi-transparent white, used for overlays

                // **App Bar and Navigation**
                AppbarBackground = "rgba(255,255,255,0.9)", // Light surface
                AppbarText = "#424242", // Dark gray, app bar text color
                DrawerBackground = "#FFF", // Light surface
                DrawerText = "#424242", // Dark gray, drawer text color

                // **Contrast Text for Primary Color**
                PrimaryContrastText = "#FFFFFF", // White, text on primary color
            },

            PaletteDark = new()
            {
                // **Primary Colors**
                Primary = primaryColor, // Deep blue, used for highlights and key elements
                Secondary = "#B0BEC5", // Light gray, secondary text color

                // **Background and Surface**
                Background = "#121212", // Standard dark mode background color
                Surface = "#1E1E1E", // Slightly lighter dark gray, used for surfaces like cards

                // **Text Colors**
                TextPrimary = "#FFFFFF", // White, primary text color
                TextSecondary = "#B0BEC5", // Light gray, secondary text color
                TextDisabled = "rgba(255,255,255,0.38)", // Semi-transparent white, disabled text color

                // **Accent Colors**
                Success = "#4CAF50", // Green, used for success messages
                Warning = "#FFC107", // Amber, used for warning messages
                Error = "#F44336", // Red, used for error messages
                Info = "#2196F3", // Blue, used for informational messages

                // **Contrast Text for Accent Colors**
                SuccessContrastText = "#FFFFFF", // White, text color for success messages
                WarningContrastText = "#000000", // Black, text color for warning messages
                ErrorContrastText = "#FFFFFF", // White, text color for error messages
                InfoContrastText = "#FFFFFF", // White, text color for informational messages

                // **Dividers and Borders**
                Divider = "rgba(255,255,255,0.12)", // Semi-transparent white, used for dividers

                // **Hover and Ripple Effects**
                HoverOpacity = 0.08, // Opacity for hover effects
                RippleOpacity = 0.12, // Opacity for ripple effects

                // **Overlay**
                OverlayDark = "rgba(0,0,0,0.5)", // Semi-transparent black, used for overlays
                OverlayLight = "rgba(30,30,30,0.4)",
                // **App Bar and Navigation**
                AppbarBackground = "#1E1E1E", // Same as surface color
                AppbarText = "#FFFFFF", // White, app bar text color
                DrawerBackground = "#1E1E1E", // Same as surface color
                DrawerText = "#FFFFFF", // White, drawer text color

                // **Contrast Text for Primary Color**
                PrimaryContrastText = "#FFFFFF", // White, text on primary color


            },
            Shadows = new()
            {
                Elevation =
                [
                    "none",
                    "0 2px 4px -1px rgba(6, 24, 44, 0.2)",
                    "0px 3px 1px -2px rgba(0,0,0,0.2),0px 2px 2px 0px rgba(0,0,0,0.14),0px 1px 5px 0px rgba(0,0,0,0.12)",
                    "0 30px 60px rgba(0,0,0,0.12)",
                    "0 6px 12px -2px rgba(50,50,93,0.25),0 3px 7px -3px rgba(0,0,0,0.3)",
                    "0 50px 100px -20px rgba(50,50,93,0.25),0 30px 60px -30px rgba(0,0,0,0.3)",
                    "0px 3px 5px -1px rgba(0,0,0,0.2),0px 6px 10px 0px rgba(0,0,0,0.14),0px 1px 18px 0px rgba(0,0,0,0.12)",
                    "0px 4px 5px -2px rgba(0,0,0,0.2),0px 7px 10px 1px rgba(0,0,0,0.14),0px 2px 16px 1px rgba(0,0,0,0.12)",
                    "0px 5px 5px -3px rgba(0,0,0,0.2),0px 8px 10px 1px rgba(0,0,0,0.14),0px 3px 14px 2px rgba(0,0,0,0.12)",
                    "0px 5px 6px -3px rgba(0,0,0,0.2),0px 9px 12px 1px rgba(0,0,0,0.14),0px 3px 16px 2px rgba(0,0,0,0.12)",
                    "0px 6px 6px -3px rgba(0,0,0,0.2),0px 10px 14px 1px rgba(0,0,0,0.14),0px 4px 18px 3px rgba(0,0,0,0.12)",
                    "0px 6px 7px -4px rgba(0,0,0,0.2),0px 11px 15px 1px rgba(0,0,0,0.14),0px 4px 20px 3px rgba(0,0,0,0.12)",
                    "0px 7px 8px -4px rgba(0,0,0,0.2),0px 12px 17px 2px rgba(0,0,0,0.14),0px 5px 22px 4px rgba(0,0,0,0.12)",
                    "0px 7px 8px -4px rgba(0,0,0,0.2),0px 13px 19px 2px rgba(0,0,0,0.14),0px 5px 24px 4px rgba(0,0,0,0.12)",
                    "0px 7px 9px -4px rgba(0,0,0,0.2),0px 14px 21px 2px rgba(0,0,0,0.14),0px 5px 26px 4px rgba(0,0,0,0.12)",
                    "0px 8px 9px -5px rgba(0,0,0,0.2),0px 15px 22px 2px rgba(0,0,0,0.14),0px 6px 28px 5px rgba(0,0,0,0.12)",
                    "0px 8px 10px -5px rgba(0,0,0,0.2),0px 16px 24px 2px rgba(0,0,0,0.14),0px 6px 30px 5px rgba(0,0,0,0.12)",
                    "0px 8px 11px -5px rgba(0,0,0,0.2),0px 17px 26px 2px rgba(0,0,0,0.14),0px 6px 32px 5px rgba(0,0,0,0.12)",
                    "0px 9px 11px -5px rgba(0,0,0,0.2),0px 18px 28px 2px rgba(0,0,0,0.14),0px 7px 34px 6px rgba(0,0,0,0.12)",
                    "0px 9px 12px -6px rgba(0,0,0,0.2),0px 19px 29px 2px rgba(0,0,0,0.14),0px 7px 36px 6px rgba(0,0,0,0.12)",
                    "0px 10px 13px -6px rgba(0,0,0,0.2),0px 20px 31px 3px rgba(0,0,0,0.14),0px 8px 38px 7px rgba(0,0,0,0.12)",
                    "0px 10px 13px -6px rgba(0,0,0,0.2),0px 21px 33px 3px rgba(0,0,0,0.14),0px 8px 40px 7px rgba(0,0,0,0.12)",
                    "0px 10px 14px -6px rgba(0,0,0,0.2),0px 22px 35px 3px rgba(0,0,0,0.14),0px 8px 42px 7px rgba(0,0,0,0.12)",
                    "0 50px 100px -20px rgba(50, 50, 93, 0.25), 0 30px 60px -30px rgba(0, 0, 0, 0.30)",
                    "2.8px 2.8px 2.2px rgba(0, 0, 0, 0.02),6.7px 6.7px 5.3px rgba(0, 0, 0, 0.028),12.5px 12.5px 10px rgba(0, 0, 0, 0.035),22.3px 22.3px 17.9px rgba(0, 0, 0, 0.042),41.8px 41.8px 33.4px rgba(0, 0, 0, 0.05),100px 100px 80px rgba(0, 0, 0, 0.07)",
                    "0px 0px 20px 0px rgba(0, 0, 0, 0.05)"
                ]
            },
            LayoutProperties = new()
            {
                DefaultBorderRadius = "8px",
                AppbarHeight = "50px",
            },
            ZIndex = new ZIndex(),
            Typography = new()
            {
                Button = new ButtonTypography
                {
                    FontSize = ".875rem",
                    FontWeight = "500",
                    LineHeight = "1.75",
                    LetterSpacing = ".02857em",
                    TextTransform = "",
                },
                Default = new DefaultTypography
                {
                    FontSize = ".8125rem",
                    FontWeight = "400",
                    LineHeight = "1.4",
                    LetterSpacing = "normal",
                    FontFamily = ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Noto Sans", "Helvetica", "Arial", "sans-serif", "Apple Color Emoji", "Segoe UI Emoji"],
                },
                H1 = new H1Typography
                {
                    FontSize = "2.2rem",
                    FontWeight = "700",
                    LineHeight = "2.5",
                    LetterSpacing = "-.01562em"
                },
                H2 = new H2Typography
                {
                    FontSize = "2rem",
                    FontWeight = "600",
                    LineHeight = "2.3",
                    LetterSpacing = "-.00833em"
                },
                H3 = new H3Typography
                {
                    FontSize = "1.75rem",
                    FontWeight = "600",
                    LineHeight = "2.2",
                    LetterSpacing = "0"
                },
                H4 = new H4Typography
                {
                    FontSize = "1.5rem",
                    FontWeight = "500",
                    LineHeight = "2",
                    LetterSpacing = ".00735em"
                },
                H5 = new H5Typography
                {
                    FontSize = "1.25rem",
                    FontWeight = "500",
                    LineHeight = "1.8",
                    LetterSpacing = "0"
                },
                H6 = new H6Typography
                {
                    FontSize = "1rem",
                    FontWeight = "500",
                    LineHeight = "1.5",
                    LetterSpacing = ".0075em"
                },
                Subtitle1 = new Subtitle1Typography
                {
                    FontSize = ".875rem",
                    FontWeight = "400",
                    LineHeight = "1.5",
                    LetterSpacing = "normal",
                    FontFamily = ["Public Sans", "Roboto", "Arial", "sans-serif"]
                },
                Subtitle2 = new Subtitle2Typography
                {
                    FontSize = ".8125rem",
                    FontWeight = "500",
                    LineHeight = "1.57",
                    LetterSpacing = ".00714em"
                },
                Body1 = new Body1Typography
                {
                    FontSize = "0.8125rem",
                    FontWeight = "400",
                    LineHeight = "1.5",
                    LetterSpacing = ".00938em"
                },
                Body2 = new Body2Typography
                {
                    FontSize = ".75rem",
                    FontWeight = "300",
                    LineHeight = "1.43",
                    LetterSpacing = ".01071em"
                },
                Caption = new CaptionTypography
                {
                    FontSize = "0.625rem",
                    FontWeight = "400",
                    LineHeight = "1.5",
                    LetterSpacing = ".03333em"
                },
                Overline = new OverlineTypography
                {
                    FontSize = "0.625rem",
                    FontWeight = "300",
                    LineHeight = "2",
                    LetterSpacing = ".08333em"
                }
            }
        };
        return theme;
    }

}
