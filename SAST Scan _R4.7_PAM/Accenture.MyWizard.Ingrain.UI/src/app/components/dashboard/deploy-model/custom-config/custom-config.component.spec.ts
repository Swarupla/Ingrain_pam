import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomConfigComponent } from './custom-config.component';

describe('CustomConfigComponent', () => {
  let component: CustomConfigComponent;
  let fixture: ComponentFixture<CustomConfigComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CustomConfigComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
