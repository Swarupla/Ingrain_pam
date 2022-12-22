import { TruncatePublicModelnamePipe } from './truncate-public-modelname.pipe';

describe('TruncatePublicModelnamePipe', () => {
  it('create an instance', () => {
    const pipe = new TruncatePublicModelnamePipe();
    expect(pipe).toBeTruthy();
  });
});
