import { TestBed } from '@angular/core/testing';

import { DecryptService } from './decrypt.service';

describe('DecryptService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: DecryptService = TestBed.get(DecryptService);
    expect(service).toBeTruthy();
  });
});
