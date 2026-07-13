import { Pressable, StyleSheet } from 'react-native';
import { Redirect, router, Stack } from 'expo-router';
import { Icon } from 'react-native-paper';
import { useAuth } from '@/hooks/useAuth';
import { theme, spacing, radius } from '@/constants/theme';

function HeaderBackButton() {
  return (
    <Pressable onPress={() => router.back()} style={headerStyles.backButton}>
      <Icon source="chevron-left" size={24} color={theme.colors.onSurface} />
    </Pressable>
  );
}

const headerStyles = StyleSheet.create({
  backButton: {
    width: 36,
    height: 36,
    borderRadius: radius.pill,
    alignItems: 'center',
    justifyContent: 'center',
    marginLeft: spacing.xs,
  },
});

export default function AppLayout() {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Redirect href="/(auth)/login" />;
  }

  return (
    <Stack
      screenOptions={{
        headerShown: false,
        contentStyle: { backgroundColor: theme.colors.background },
      }}
    >
      <Stack.Screen name="(tabs)" />
      <Stack.Screen
        name="cards/[cardId]"
        options={{
          headerShown: true,
          headerStyle: { backgroundColor: theme.colors.surface },
          headerTintColor: theme.colors.onSurface,
          headerShadowVisible: false,
          title: 'Detalle de tarjeta',
        }}
      />
      <Stack.Screen
        name="cards/add"
        options={{
          headerShown: true,
          headerStyle: { backgroundColor: theme.colors.surface },
          headerTintColor: theme.colors.onSurface,
          headerShadowVisible: false,
          title: 'Agregar tarjeta',
          presentation: 'modal',
        }}
      />
      <Stack.Screen
        name="household/invite"
        options={{
          headerShown: true,
          headerStyle: { backgroundColor: theme.colors.surface },
          headerTintColor: theme.colors.onSurface,
          headerShadowVisible: false,
          title: 'Invitar miembro',
          presentation: 'modal',
        }}
      />
      <Stack.Screen
        name="household/split-rules/index"
        options={{
          headerShown: true,
          headerStyle: { backgroundColor: theme.colors.surface },
          headerTintColor: theme.colors.onSurface,
          headerShadowVisible: false,
          headerLeft: () => <HeaderBackButton />,
          title: 'Reglas de split',
        }}
      />
      <Stack.Screen
        name="household/split-rules/create"
        options={{
          headerShown: true,
          headerStyle: { backgroundColor: theme.colors.surface },
          headerTintColor: theme.colors.onSurface,
          headerShadowVisible: false,
          title: 'Nueva regla',
          presentation: 'modal',
        }}
      />
      <Stack.Screen
        name="transactions/new"
        options={{
          headerShown: true,
          headerStyle: { backgroundColor: theme.colors.surface },
          headerTintColor: theme.colors.onSurface,
          headerShadowVisible: false,
          title: 'Nueva compra',
          presentation: 'modal',
        }}
      />
    </Stack>
  );
}
