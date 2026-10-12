import { Routes } from '@angular/router';
import { authGuard, guestGuard, usersGuard } from './account/auth.guard';
import { CheckEmailPage } from './account/check-email-page/check-email-page';
import { ConfirmEmailPage } from './account/confirm-email-page/confirm-email-page';
import { ForgotPasswordPage } from './account/forgot-password-page/forgot-password-page';
import { LoginPage } from './account/login-page/login-page';
import { ResetPasswordPage } from './account/reset-password-page/reset-password-page';
import { ChatPage } from './chats/chat-page/chat-page';
import { HomePage } from './home/home-page/home-page';
import { Shell } from './layout/shell/shell';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [guestGuard] },
  { path: 'check-email', component: CheckEmailPage, canActivate: [guestGuard] },
  { path: 'confirm-email', component: ConfirmEmailPage, canActivate: [guestGuard] },
  { path: 'forgot-password', component: ForgotPasswordPage, canActivate: [guestGuard] },
  { path: 'reset-password', component: ResetPasswordPage, canActivate: [guestGuard] },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', component: HomePage },
      { path: 'chat/:id', component: ChatPage },
      {
        path: 'users',
        canActivate: [usersGuard],
        loadComponent: () => import('./users/users-page/users-page').then((m) => m.UsersPage),
      },
      {
        path: 'users/:id',
        canActivate: [usersGuard],
        loadComponent: () => import('./users/user-access-page/user-access-page').then((m) => m.UserAccessPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
