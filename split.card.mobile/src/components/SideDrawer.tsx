import { useEffect, useRef } from 'react';
import { Animated, Dimensions, Pressable, StyleSheet, View } from 'react-native';
import { Avatar, Divider, Icon, Portal, Text } from 'react-native-paper';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { theme, spacing, radius } from '@/constants/theme';
import type { User } from '@/types/user';

const DRAWER_WIDTH = Math.min(320, Dimensions.get('window').width * 0.84);
const ANIMATION_MS = 220;

interface DrawerItem {
  label: string;
  icon: string;
  path: string;
}

interface SideDrawerProps {
  visible: boolean;
  onClose: () => void;
  user: User | null;
  onLogout: () => void;
}

/**
 * Portal + Animated, not react-native-paper's Drawer — same reasoning as Dropdown's
 * bottom sheet: a Portal-based overlay sidesteps any anchor/positioning quirks entirely,
 * and gives full control over the slide-in-from-left animation and styling. Backdrop and
 * panel animate together (opacity + translateX) so closing doesn't visually snap.
 */
export function SideDrawer({ visible, onClose, user, onLogout }: SideDrawerProps) {
  const insets = useSafeAreaInsets();
  const translateX = useRef(new Animated.Value(-DRAWER_WIDTH)).current;
  const backdropOpacity = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.timing(translateX, {
        toValue: visible ? 0 : -DRAWER_WIDTH,
        duration: ANIMATION_MS,
        useNativeDriver: true,
      }),
      Animated.timing(backdropOpacity, {
        toValue: visible ? 1 : 0,
        duration: ANIMATION_MS,
        useNativeDriver: true,
      }),
    ]).start();
  }, [visible, translateX, backdropOpacity]);

  function navigateAndClose(path: string) {
    onClose();
    router.push(path);
  }

  // Accesos reales a pantallas que ya existen — no items de relleno para "futuras
  // pantallas" sin funcionalidad detrás (eso sería un placeholder falso). Agregar más acá
  // a medida que se construyan pantallas nuevas.
  const items: DrawerItem[] = [
    { label: 'Reglas de split', icon: 'table-split-cell', path: '/(app)/household/split-rules' },
    ...(user?.role === 'Owner'
      ? [
          { label: 'Invitar miembro', icon: 'account-plus-outline', path: '/(app)/household/invite' },
          { label: 'Agregar tarjeta', icon: 'credit-card-plus-outline', path: '/(app)/cards/add' },
        ]
      : []),
  ];

  return (
    <Portal>
      <Animated.View
        style={[styles.backdrop, { opacity: backdropOpacity }]}
        pointerEvents={visible ? 'auto' : 'none'}
      >
        <Pressable style={StyleSheet.absoluteFill} onPress={onClose} />
      </Animated.View>

      <Animated.View
        style={[
          styles.drawer,
          { paddingTop: insets.top + spacing.md, transform: [{ translateX }] },
        ]}
        pointerEvents={visible ? 'auto' : 'none'}
      >
        <View style={styles.header}>
          <Avatar.Text size={48} label={(user?.name ?? '?').charAt(0).toUpperCase()} />
          <View style={styles.headerText}>
            <Text variant="titleMedium" style={styles.userName} numberOfLines={1}>
              {user?.name}
            </Text>
            <Text variant="bodySmall" style={styles.userEmail} numberOfLines={1}>
              {user?.email}
            </Text>
          </View>
        </View>

        <Divider style={styles.divider} />

        <View style={styles.itemsList}>
          {items.map((item) => (
            <Pressable key={item.label} style={styles.item} onPress={() => navigateAndClose(item.path)}>
              <Icon source={item.icon} size={22} color={theme.colors.onSurface} />
              <Text variant="bodyLarge" style={styles.itemLabel}>
                {item.label}
              </Text>
            </Pressable>
          ))}
        </View>

        <View style={styles.footer}>
          <Divider style={styles.divider} />
          <Pressable style={styles.item} onPress={onLogout}>
            <Icon source="logout" size={22} color={theme.colors.error} />
            <Text variant="bodyLarge" style={[styles.itemLabel, styles.logoutLabel]}>
              Cerrar sesión
            </Text>
          </Pressable>
        </View>
      </Animated.View>
    </Portal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    zIndex: 20,
  },
  drawer: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    left: 0,
    width: DRAWER_WIDTH,
    backgroundColor: theme.colors.surface,
    zIndex: 21,
    paddingHorizontal: spacing.md,
    borderTopRightRadius: radius.lg,
    borderBottomRightRadius: radius.lg,
    elevation: 16,
    shadowColor: '#000',
    shadowOffset: { width: 4, height: 0 },
    shadowOpacity: 0.35,
    shadowRadius: 16,
  },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, paddingBottom: spacing.md },
  headerText: { flex: 1 },
  userName: { color: theme.colors.onSurface },
  userEmail: { color: theme.colors.onSurfaceVariant, marginTop: 2 },
  divider: { backgroundColor: theme.colors.outlineVariant, marginVertical: spacing.sm },
  itemsList: { gap: spacing.xs },
  item: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.xs,
    borderRadius: radius.sm,
  },
  itemLabel: { color: theme.colors.onSurface },
  footer: { marginTop: 'auto', marginBottom: spacing.lg },
  logoutLabel: { color: theme.colors.error },
});
