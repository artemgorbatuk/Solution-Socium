import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './account/auth.guard';
import { LoginPage } from './account/login-page/login-page';
import { ChatPage } from './chats/chat-page/chat-page';
import { HomePage } from './home/home-page/home-page';
import { Shell } from './layout/shell/shell';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [guestGuard] },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', component: HomePage },
      { path: 'chat/:id', component: ChatPage },
    ],
  },
  { path: '**', redirectTo: '' },
];
