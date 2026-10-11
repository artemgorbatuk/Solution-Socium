import { Component, DestroyRef, OnDestroy, effect, inject, input, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';
import { FileApi } from '../../files/file-api';
import { isImageContentType } from '../../files/file.models';
import { formatFileSize } from '../../shared/format/format-file-size';
import { MessageAttachmentModel } from '../message.models';

@Component({
  selector: 'app-message-attachment',
  templateUrl: './message-attachment.html',
  styleUrl: './message-attachment.css',
})
export class MessageAttachment implements OnDestroy {
  private readonly fileApi = inject(FileApi);
  private loadSubscription?: Subscription;
  private loadedId?: string;

  readonly attachment = input.required<MessageAttachmentModel>();

  protected readonly objectUrl = signal<string | null>(null);
  protected readonly loadError = signal(false);

  constructor() {
    inject(DestroyRef).onDestroy(() => this.loadSubscription?.unsubscribe());
    effect(() => {
      const attachment = this.attachment();
      untracked(() => this.load(attachment));
    });
  }

  protected isImage(contentType: string): boolean {
    return isImageContentType(contentType);
  }

  protected sizeLabel(bytes: number): string {
    return formatFileSize(bytes);
  }

  ngOnDestroy(): void {
    this.revoke();
  }

  protected download(): void {
    const url = this.objectUrl();
    if (!url) {
      return;
    }

    const link = document.createElement('a');
    link.href = url;
    link.download = this.attachment().fileName;
    link.click();
  }

  private load(attachment: MessageAttachmentModel): void {
    if (attachment.id === this.loadedId && this.objectUrl()) {
      return;
    }

    this.loadedId = attachment.id;
    this.loadError.set(false);
    this.revoke();
    this.loadSubscription?.unsubscribe();
    this.loadSubscription = this.fileApi.downloadBlob(attachment.id).subscribe({
      next: (blob) => this.objectUrl.set(URL.createObjectURL(blob)),
      error: () => this.loadError.set(true),
    });
  }

  private revoke(): void {
    const url = this.objectUrl();
    if (url) {
      URL.revokeObjectURL(url);
      this.objectUrl.set(null);
    }
  }
}
