import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdPublishModelComponent } from './ad-publish-model.component';

describe('AdPublishModelComponent', () => {
  let component: AdPublishModelComponent;
  let fixture: ComponentFixture<AdPublishModelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdPublishModelComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdPublishModelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
