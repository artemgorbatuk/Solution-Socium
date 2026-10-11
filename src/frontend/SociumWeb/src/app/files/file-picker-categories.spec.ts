import {
  fileInputAccept,
  isAllowedAttachment,
  openFilePickerTypes,
} from './file-picker-categories';

describe('filePickerCategories', () => {
  it('OpenPickerTypes_Order_ShouldBeAllImagesOfficePdfCode', () => {
    expect(openFilePickerTypes.map((type) => type.description)).toEqual([
      'Все',
      'Картинки',
      'Office',
      'PDF',
      'Код',
    ]);
  });

  it('OpenPickerTypes_Code_ShouldAcceptCsharpExtension', () => {
    const code = openFilePickerTypes.find((type) => type.description === 'Код')!;
    expect(code.accept['text/x-csharp']).toContain('.cs');
  });

  it('FileInputAccept_Fallback_ShouldIncludePdfAndDocx', () => {
    expect(fileInputAccept).toContain('.pdf');
    expect(fileInputAccept).toContain('.docx');
    expect(fileInputAccept).toContain('.cs');
  });

  it('IsAllowed_WithCsAndZip_ShouldAllowOnlyCs', () => {
    const cs = new File(['class A {}'], 'Program.cs', { type: '' });
    const zip = new File(['x'], 'a.zip', { type: '' });

    expect(isAllowedAttachment(cs)).toBe(true);
    expect(isAllowedAttachment(zip)).toBe(false);
  });
});
