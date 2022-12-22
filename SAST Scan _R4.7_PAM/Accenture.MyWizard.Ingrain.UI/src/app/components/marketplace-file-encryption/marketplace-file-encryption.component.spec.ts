import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MarketplaceFileEncryptionComponent } from './marketplace-file-encryption.component';

describe('MarketplaceFileEncryptionComponent', () => {
  let component: MarketplaceFileEncryptionComponent;
  let fixture: ComponentFixture<MarketplaceFileEncryptionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MarketplaceFileEncryptionComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(MarketplaceFileEncryptionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
