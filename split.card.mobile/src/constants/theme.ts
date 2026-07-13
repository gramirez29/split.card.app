import { MD3DarkTheme } from 'react-native-paper';
import type { MD3Theme } from 'react-native-paper';

// Dark, card-based aesthetic (reference: Avast One dashboard) — deep navy background,
// slightly-lighter navy surfaces for cards, bright blue accent for primary actions and
// the active tab. Extends MD3DarkTheme (not MD3LightTheme) so every unlisted color role
// (shadows, disabled states, etc.) already has sane dark defaults.
export const theme: MD3Theme = {
  ...MD3DarkTheme,
  colors: {
    ...MD3DarkTheme.colors,
    primary: '#4C8DFF',
    onPrimary: '#00204D',
    primaryContainer: '#0F3D8C',
    onPrimaryContainer: '#D6E4FF',
    secondary: '#7CE0D3',
    onSecondary: '#00332C',
    secondaryContainer: '#0B4A40',
    onSecondaryContainer: '#B6F5E9',
    background: '#0B1220',
    onBackground: '#E7ECF5',
    surface: '#131B2C',
    onSurface: '#E7ECF5',
    surfaceVariant: '#1B2438',
    onSurfaceVariant: '#9AA7BF',
    outline: '#2A3550',
    outlineVariant: '#1F2A42',
    error: '#FF6B6B',
    onError: '#3D0A0A',
    errorContainer: '#5C1A1A',
    onErrorContainer: '#FFDAD6',
    elevation: {
      level0: 'transparent',
      level1: '#141C2E',
      level2: '#182034',
      level3: '#1C263B',
      level4: '#1E2840',
      level5: '#212B46',
    },
  },
};

// Shared design tokens used outside Paper's theme object (e.g. StyleSheet.create calls
// in screens that aren't fully migrated to Paper components yet).
export const spacing = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
} as const;

export const radius = {
  sm: 8,
  md: 16,
  lg: 20,
  pill: 999,
} as const;

// Not part of Paper's MD3Theme.colors (which has a fixed, typed set of color roles) — a
// separate export so screens can use an amber "heads up" accent for alerts (cuotas por
// vencer, corte/pago próximo) without it being confused with theme.colors.error, which
// reads as more alarming than an alert genuinely is.
//
// Fixed a real contrast bug here: onWarningContainer was originally a DARK brown
// (#4A3400), which is the right choice for a LIGHT warningContainer background — but
// warningContainer here is dark (this is a dark theme), so dark-on-dark was nearly
// unreadable. Same pattern as primaryContainer/onPrimaryContainer above: dark container,
// light text.
export const alertColors = {
  warning: '#F0B429',
  warningContainer: '#3D2E0A',
  onWarningContainer: '#FFE1A8',
} as const;
