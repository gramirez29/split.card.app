import { MD3LightTheme } from 'react-native-paper';
import type { MD3Theme } from 'react-native-paper';

export const theme: MD3Theme = {
  ...MD3LightTheme,
  colors: {
    ...MD3LightTheme.colors,
    primary: '#3A5BFF',
    onPrimary: '#FFFFFF',
    primaryContainer: '#DDE3FF',
    onPrimaryContainer: '#00114F',
    secondary: '#5B5E77',
    background: '#FBF8FF',
    surface: '#FBF8FF',
    error: '#BA1A1A',
  },
};
