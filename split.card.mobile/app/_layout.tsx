import { Stack } from 'expo-router';
import { PaperProvider } from 'react-native-paper';
import { MaterialCommunityIcons } from '@expo/vector-icons';
import { theme } from '@/constants/theme';

export default function RootLayout() {
  return (
    <PaperProvider theme={theme} settings={{ icon: (props) => <MaterialCommunityIcons {...props} /> }}>
      <Stack screenOptions={{ headerShown: false }} />
    </PaperProvider>
  );
}
