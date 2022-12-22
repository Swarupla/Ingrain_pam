import { TestBed } from '@angular/core/testing';

import { RecommendedAiService } from './recommended-ai.service';

describe('RecommendedAiService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: RecommendedAiService = TestBed.get(RecommendedAiService);
    expect(service).toBeTruthy();
  });
});
