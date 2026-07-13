import { View } from 'react-native';
import { Tabs, router } from 'expo-router';
import { Icon } from 'react-native-paper';
import { theme } from '@/constants/theme';
import { TopAppBar } from '@/components/TopAppBar';
import { SideDrawer } from '@/components/SideDrawer';
import { useUiStore } from '@/store/uiStore';
import { useAuth } from '@/hooks/useAuth';

export default function TabsLayout() {
  const { isDrawerOpen, openDrawer, closeDrawer } = useUiStore();
  const { user, logout } = useAuth();

  async function handleLogout() {
    closeDrawer();
    await logout();
    router.replace('/(auth)/login');
  }

  return (
    <View style={{ flex: 1 }}>
      <Tabs
        screenOptions={{
          headerShown: false,
          tabBarShowLabel: true,
          tabBarActiveTintColor: theme.colors.primary,
          tabBarInactiveTintColor: theme.colors.onSurfaceVariant,
          tabBarStyle: {
            backgroundColor: theme.colors.surface,
            borderTopWidth: 0,
            position: 'absolute',
            left: 16,
            right: 16,
            bottom: 16,
            height: 64,
            borderRadius: 24,
            paddingTop: 8,
            paddingBottom: 8,
            elevation: 8,
            shadowColor: '#000',
            shadowOffset: { width: 0, height: 4 },
            shadowOpacity: 0.3,
            shadowRadius: 12,
          },
          tabBarItemStyle: {
            borderRadius: 16,
          },
          tabBarLabelStyle: {
            fontSize: 11,
            fontWeight: '600',
          },
        }}
      >
        <Tabs.Screen
          name="index"
          options={{
            title: 'Home',
            tabBarIcon: ({ color, size }) => <Icon source="view-dashboard-outline" color={color} size={size} />,
          }}
        />
        <Tabs.Screen
          name="cards"
          options={{
            title: 'Cards',
            tabBarIcon: ({ color, size }) => <Icon source="credit-card-outline" color={color} size={size} />,
          }}
        />
        <Tabs.Screen
          name="household"
          options={{
            title: 'Household',
            tabBarIcon: ({ color, size }) => <Icon source="account-group-outline" color={color} size={size} />,
          }}
        />
        <Tabs.Screen
          name="account"
          options={{
            title: 'Account',
            tabBarIcon: ({ color, size }) => <Icon source="account-circle-outline" color={color} size={size} />,
          }}
        />
      </Tabs>

      <TopAppBar onMenuPress={openDrawer} />
      <SideDrawer visible={isDrawerOpen} onClose={closeDrawer} user={user} onLogout={handleLogout} />
    </View>
  );
}
