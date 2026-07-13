import { View, StyleSheet } from 'react-native';
import { Avatar, Button, Divider, Text } from 'react-native-paper';
import { router } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '@/hooks/useAuth';
import { theme, spacing, radius } from '@/constants/theme';
import { APP_BAR_HEIGHT } from '@/components/TopAppBar';

export default function AccountScreen() {
  const insets = useSafeAreaInsets();
  const { user, logout } = useAuth();

  async function handleLogout() {
    await logout();
    router.replace('/(auth)/login');
  }

  return (
    <View style={[styles.container, { paddingTop: insets.top + APP_BAR_HEIGHT + spacing.md }]}>
      <Text variant="headlineSmall" style={styles.title}>
        Cuenta
      </Text>

      <View style={styles.profileCard}>
        <Avatar.Text size={56} label={(user?.name ?? '?').charAt(0).toUpperCase()} />
        <View style={styles.profileInfo}>
          <Text variant="titleMedium" style={styles.profileName}>
            {user?.name}
          </Text>
          <Text variant="bodySmall" style={styles.profileEmail}>
            {user?.email}
          </Text>
        </View>
      </View>

      <View style={styles.infoRow}>
        <Text style={styles.infoLabel}>Rol</Text>
        <Text style={styles.infoValue}>{user?.role}</Text>
      </View>

      <Divider style={styles.divider} />

      <Button
        mode="outlined"
        icon="logout"
        onPress={handleLogout}
        textColor={theme.colors.error}
        style={styles.logoutButton}
      >
        Cerrar sesión
      </Button>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.colors.background, padding: spacing.md },
  title: { color: theme.colors.onBackground, fontWeight: '700', marginBottom: spacing.lg },
  profileCard: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    backgroundColor: theme.colors.surface,
    borderRadius: radius.lg,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  profileInfo: { flex: 1 },
  profileName: { color: theme.colors.onSurface },
  profileEmail: { color: theme.colors.onSurfaceVariant, marginTop: 2 },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  infoLabel: { color: theme.colors.onSurfaceVariant },
  infoValue: { color: theme.colors.onSurface, fontWeight: '600' },
  divider: { marginVertical: spacing.lg, backgroundColor: theme.colors.outlineVariant },
  logoutButton: { borderRadius: radius.pill, borderColor: theme.colors.error },
});
