import { Routes } from '@angular/router';
import { ChatPage } from './chats/chat-page/chat-page';
import { HomePage } from './home/home-page/home-page';

export const routes: Routes = [
  { path: '', component: HomePage },
  { path: 'chat/:id', component: ChatPage },
  { path: '**', redirectTo: '' },
];
